// Named aliases over the generated OpenAPI types (schema.d.ts, produced by `npm run contract:generate`).
// Hand-written code imports from here, never from schema.d.ts directly; if the API contract changes
// incompatibly, `npm run build` fails here instead of at runtime.
import type { components } from './schema'

type Schemas = components['schemas']

export type RegisterRequest = Schemas['RegisterRequest']
export type RegisterResponse = Schemas['RegisterResponse']
export type LoginRequest = Schemas['LoginRequest']
export type LoginResponse = Schemas['LoginResponse']
export type UserDto = Schemas['UserDto']
export type AccountSummary = Schemas['AccountSummary']
export type AccountListResponse = Schemas['AccountListResponse']
export type CapabilitiesResponse = Schemas['CapabilitiesResponse']
export type CapabilityDto = Schemas['CapabilityDto']
export type ProjectDto = Schemas['ProjectDto']
export type ProjectListResponse = Schemas['ProjectListResponse']
export type CreateProjectRequest = Schemas['CreateProjectRequest']

/** Every error response; branch on `code`, never on `title`/`detail`. */
export type ProblemDetails = Schemas['ProblemDetails']
export type ProblemCode = ProblemDetails['code']
