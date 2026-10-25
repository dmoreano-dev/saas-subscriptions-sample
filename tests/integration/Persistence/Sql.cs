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
