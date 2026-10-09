import type { components } from './api/schema'

/**
 * 前台與後台共用的 API 型別。
 *
 * `paths`、`components` 由後端 OpenAPI 文件產生（`pnpm gen:api`），禁止手改（docs/FRONTEND.md §5.2）。
 */
export type { components, paths } from './api/schema'

/** 取得後端 DTO 的型別，例：`ApiSchema<'TcmClinicApiUseCasesNavigationMyMenuNodeDto'>`。 */
export type ApiSchema<TName extends keyof components['schemas']> = components['schemas'][TName]
