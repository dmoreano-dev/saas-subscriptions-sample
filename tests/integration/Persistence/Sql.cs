using Npgsql;

namespace Saas.Subscription.Sample.IntegrationTests.Persistence;

internal static class Sql
{
    public static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static async Task<int> ExecuteAsync(NpgsqlConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = Build(connection, sql, parameters);
        return await command.ExecuteNonQueryAsync();
    }

    public static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = Build(connection, sql, parameters);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    public static async Task<List<string>> ColumnAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = Build(connection, sql, []);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    /// <summary>Asserts the statement fails with the given PostgreSQL SQLSTATE (23505 unique, 23503 FK, 23514 check, 42501 permission).</summary>
    public static async Task AssertSqlStateAsync(string expectedSqlState, Func<Task> action)
    {
        var exception = await Assert.ThrowsAnyAsync<PostgresException>(action);
        Assert.Equal(expectedSqlState, exception.SqlState);
    }

    public static Guid NewId() => Guid.CreateVersion7();

    public static Task InsertUserAsync(NpgsqlConnection connection, Guid id, string email, string? normalizedEmail = null, NpgsqlTransaction? transaction = null) =>
        InsertAsync(
            connection,
            transaction,
            "INSERT INTO users (id, email, normalized_email, created_at, updated_at) VALUES (@id, @email, @normalized, now(), now())",
            ("id", id), ("email", email), ("normalized", normalizedEmail ?? email.Trim().ToLowerInvariant()));

    public static Task InsertPasswordCredentialAsync(
        NpgsqlConnection connection,
        Guid userId,
        string algorithm = "argon2id",
        string encodedHash = "$argon2id$placeholder") =>
        InsertAsync(
            connection,
            transaction: null,
            "INSERT INTO password_credentials (user_id, algorithm, encoded_hash, created_at, updated_at) VALUES (@user, @algorithm, @hash, now(), now())",
            ("user", userId), ("algorithm", algorithm), ("hash", encodedHash));

    public static Task InsertAccountAsync(NpgsqlConnection connection, Guid id, string type, Guid? personalOwnerUserId, string status = "Active", NpgsqlTransaction? transaction = null) =>
        InsertAsync(
            connection,
            transaction,
            "INSERT INTO accounts (id, type, status, name, personal_owner_user_id, created_at, updated_at) VALUES (@id, @type, @status, 'Test', @owner, now(), now())",
            ("id", id), ("type", type), ("status", status), ("owner", personalOwnerUserId));

    public static async Task<Guid> InsertMembershipAsync(NpgsqlConnection connection, Guid accountId, Guid userId, NpgsqlTransaction? transaction = null)
    {
        var id = NewId();
        await InsertAsync(
            connection,
            transaction,
            "INSERT INTO memberships (id, account_id, user_id, created_at, updated_at) VALUES (@id, @account, @user, now(), now())",
            ("id", id), ("account", accountId), ("user", userId));
        return id;
    }

    public static Task InsertRoleAsync(NpgsqlConnection connection, string key, string scope) =>
        InsertAsync(connection, transaction: null, "INSERT INTO roles (key, scope, name) VALUES (@key, @scope, @key)", ("key", key), ("scope", scope));

    public static Task InsertRolePermissionAsync(NpgsqlConnection connection, string roleKey, string permissionKey, string scope) =>
        InsertAsync(
            connection,
            transaction: null,
            "INSERT INTO role_permissions (role_key, permission_key, scope) VALUES (@role, @permission, @scope)",
            ("role", roleKey), ("permission", permissionKey), ("scope", scope));

    public static Task InsertMembershipRoleAsync(NpgsqlConnection connection, Guid membershipId, string roleKey, string roleScope = "Account") =>
        InsertAsync(
            connection,
            transaction: null,
            "INSERT INTO membership_roles (membership_id, role_key, role_scope, created_at) VALUES (@membership, @role, @scope, now())",
            ("membership", membershipId), ("role", roleKey), ("scope", roleScope));

    public static Task InsertPlatformRoleAssignmentAsync(NpgsqlConnection connection, Guid userId, string roleKey, string roleScope = "Platform") =>
        InsertAsync(
            connection,
            transaction: null,
            "INSERT INTO platform_role_assignments (user_id, role_key, role_scope, created_at) VALUES (@user, @role, @scope, now())",
            ("user", userId), ("role", roleKey), ("scope", roleScope));

    public static async Task<Guid> InsertProjectAsync(NpgsqlConnection connection, Guid accountId, string name)
    {
        var id = NewId();
        await InsertAsync(
            connection,
            transaction: null,
            "INSERT INTO projects (id, account_id, name, created_at, updated_at) VALUES (@id, @account, @name, now(), now())",
            ("id", id), ("account", accountId), ("name", name));
        return id;
    }

    /// <summary>The id of a seeded (or otherwise inserted) version of a plan.</summary>
    public static Task<Guid> PlanVersionIdAsync(NpgsqlConnection connection, string planKey, int version = 1) =>
        ScalarAsync<Guid>(
            connection,
            "SELECT pv.id FROM plan_versions pv JOIN plans p ON p.id = pv.plan_id WHERE p.key = @key AND pv.version = @version",
            ("key", planKey), ("version", version));

    /// <summary>A subscription whose period runs [periodStart, periodEnd); access ends at accessUntil (periodEnd by default).</summary>
    public static async Task<Guid> InsertSubscriptionAsync(
        NpgsqlConnection connection,
        Guid accountId,
        Guid planVersionId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string status = "Active",
        DateTimeOffset? accessUntil = null,
        DateTimeOffset? endedAt = null)
    {
        var id = NewId();
        await InsertAsync(
            connection,
            transaction: null,
            """
            INSERT INTO subscriptions (id, account_id, plan_version_id, status, current_period_start, current_period_end, access_until, started_at, ended_at, created_at, updated_at)
            VALUES (@id, @account, @version, @status, @start, @end, @access, @start, @ended, now(), now())
            """,
            ("id", id), ("account", accountId), ("version", planVersionId), ("status", status),
            ("start", periodStart), ("end", periodEnd), ("access", accessUntil ?? periodEnd), ("ended", endedAt));
        return id;
    }

    /// <summary>A scheduled change; resolved_at is set for every status except Pending.</summary>
    public static async Task<Guid> InsertSubscriptionChangeAsync(
        NpgsqlConnection connection,
        Guid subscriptionId,
        Guid accountId,
        string kind,
        Guid? targetPlanVersionId,
        DateTimeOffset effectiveAt,
        string status = "Pending")
    {
        var id = NewId();
        await InsertAsync(
            connection,
            transaction: null,
            """
            INSERT INTO subscription_changes (id, subscription_id, account_id, kind, target_plan_version_id, effective_at, status, requested_at, resolved_at)
            VALUES (@id, @subscription, @account, @kind, @target, @effective, @status, now(), CASE WHEN @status = 'Pending' THEN NULL ELSE now() END)
            """,
            ("id", id), ("subscription", subscriptionId), ("account", accountId), ("kind", kind),
            ("target", targetPlanVersionId), ("effective", effectiveAt), ("status", status));
        return id;
    }

    public static Task<int> DeleteUserAsync(NpgsqlConnection connection, Guid id) =>
        ExecuteAsync(connection, "DELETE FROM users WHERE id = @id", ("id", id));

    public static Task<int> DeleteAccountAsync(NpgsqlConnection connection, Guid id) =>
        ExecuteAsync(connection, "DELETE FROM accounts WHERE id = @id", ("id", id));

    public static Task<long> CountMembershipsAsync(NpgsqlConnection connection, Guid userId) =>
        ScalarAsync<long>(connection, "SELECT count(*) FROM memberships WHERE user_id = @id", ("id", userId));

    private static async Task InsertAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = Build(connection, sql, parameters);
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync();
    }

    private static NpgsqlCommand Build(NpgsqlConnection connection, string sql, (string Name, object? Value)[] parameters)
    {
        var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }
}
