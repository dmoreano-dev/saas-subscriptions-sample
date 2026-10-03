import { execFileSync, spawn, type ChildProcess } from 'node:child_process'
import { generateKeyPairSync, randomBytes } from 'node:crypto'
import { resolve } from 'node:path'

// Starts the real backend for the browser scenarios: PostgreSQL 17 in Docker, the Migrator, and the API with a
// throwaway ES256 key generated in memory (nothing secret is committed or written to disk). The frontend
// (`vite preview`, started by playwright.config.ts) proxies /api to API_URL.
export const API_PORT = 5290
export const API_URL = `http://127.0.0.1:${API_PORT}`

const repoRoot = resolve(import.meta.dirname, '../../..')
const apiProject = resolve(repoRoot, 'src/backend/Saas.Subscription.Sample.Api')
const migratorProject = resolve(repoRoot, 'src/backend/Saas.Subscription.Sample.Migrator')
const containerName = `saas-browser-pg-${randomBytes(4).toString('hex')}`
const dbPassword = randomBytes(12).toString('hex')

const sleep = (ms: number) => new Promise((done) => setTimeout(done, ms))

async function waitFor(description: string, probe: () => Promise<boolean> | boolean, timeoutMs: number) {
  const deadline = Date.now() + timeoutMs
  while (Date.now() < deadline) {
    try {
      if (await probe()) return
    } catch {
      // not ready yet
    }
    await sleep(500)
  }
  throw new Error(`Timed out waiting for ${description}`)
}

export default async function globalSetup() {
  execFileSync('docker', [
    'run', '--rm', '-d', '--name', containerName,
    '-e', `POSTGRES_PASSWORD=${dbPassword}`, '-e', 'POSTGRES_DB=saas_sample',
    '-p', '127.0.0.1::5432', 'postgres:17',
  ], { stdio: 'pipe' })
  const dbPort = execFileSync('docker', ['port', containerName, '5432/tcp']).toString().trim().split(':').pop()
  const connectionString = `Host=127.0.0.1;Port=${dbPort};Database=saas_sample;Username=postgres;Password=${dbPassword}`

  let api: ChildProcess | undefined
  const teardown = async () => {
    api?.kill('SIGTERM')
    try {
      execFileSync('docker', ['rm', '-f', containerName], { stdio: 'pipe' })
    } catch {
      // already gone
    }
  }

  try {
    // pg_isready is true during the image's init phase too, so probe with a real query over TCP.
    await waitFor('PostgreSQL', () => {
      execFileSync('docker', ['exec', containerName, 'psql', '-U', 'postgres', '-h', '127.0.0.1', '-d', 'saas_sample', '-c', 'select 1'], { stdio: 'pipe' })
      return true
    }, 60_000)

    execFileSync('dotnet', ['build', apiProject, '-v', 'q'], { stdio: 'pipe' })
    execFileSync('dotnet', ['run', '--project', migratorProject, '--no-launch-profile'], {
      stdio: 'pipe',
      env: { ...process.env, ConnectionStrings__appdb: connectionString },
    })

    const { privateKey } = generateKeyPairSync('ec', { namedCurve: 'prime256v1' })
    api = spawn('dotnet', ['run', '--project', apiProject, '--no-build', '--no-launch-profile'], {
      stdio: 'ignore',
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: API_URL,
        Database__ConnectionString: connectionString,
        Authentication__SigningKey: privateKey.export({ type: 'pkcs8', format: 'pem' }).toString(),
        Authentication__SigningKeyId: 'browser-test',
      },
    })
    await waitFor('the API', async () => (await fetch(`${API_URL}/health/live`)).ok, 60_000)
  } catch (error) {
    await teardown()
    throw error
  }
  return teardown
}
