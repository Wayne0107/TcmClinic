# Conventional Commits 1.0.0

## Project-Specific Rules

以下專案規則優先於本檔下方的 Conventional Commits 規格說明。類型沿用 UDS 選項 `.standards/options/traditional-chinese.ai.yaml`（源專案 2026-10-06 改採 UDS 流程時訂定）。

- **類型（type）用繁中**，對照 Conventional Commits 英文類型：

  | 類型 | 英文對照 | 用途 |
  |------|---------|------|
  | `新增` | feat | 新功能 |
  | `修正` | fix | 錯誤修正 |
  | `重構` | refactor | 程式碼重構（無功能變更） |
  | `文件` | docs | 文件與規格（規格的撰寫與修訂、ADR 也用這個） |
  | `樣式` | style | 格式調整（無邏輯變更） |
  | `測試` | test | 新增或更新測試 |
  | `效能` | perf | 效能改善 |
  | `建置` | build | 建置系統或相依套件 |
  | `整合` | ci | CI/CD 流程變更 |
  | `維護` | chore | 其他維護工作 |
  | `回退` | revert | 回退先前的 commit |
  | `安全` | — | 安全性修正 |

- **範圍（scope）**：SPEC 編號（如 `058`）或模組名（如 `web`、`流程`）；同一 repo 內保持一致。
- **描述與本文用繁體中文**。主旨（第一行）不超過 72 字元，任務編號、測試數等細節寫在本文。
- **頁腳 token 用英文**：實作類 commit 加 `Refs: SPEC-NNN`；破壞性變更用 `BREAKING CHANGE:`。token 後的說明可用繁中。
- **每個 commit 都必須建置成功、測試全綠**（UDS checkin）；不再提交只含失敗測試的紅燈 commit。TDD 紅燈的證據改記在規格的「實作紀錄」（見 `docs/TESTING.md` §1）。
- **不加 AI 共同作者尾行**（`Co-Authored-By: …`）。
- **合併採 squash**（UDS 選項 `squash-merge`）：經 PR 合併回 master，合併 commit 以本規則撰寫並附 PR 編號，例：`新增(058): 帳號維護 (#12)`。

## Summary

The Conventional Commits specification is a lightweight convention on top of commit messages. It provides an easy set of rules for creating an explicit commit history; which makes it easier to write automated tools on top of. This convention dovetails with [SemVer](http://semver.org/), by describing the features, fixes, and breaking changes made in commit messages.

The commit message should be structured as follows:

* * * * *

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

* * * * *

The commit contains the following structural elements, to communicate intent to the consumers of your library:

1. **fix:**  a commit of the *type* patches a bug in your codebase (this correlates with [`PATCH`](http://semver.org/#summary) in Semantic Versioning).`fix`
2. **feat:**  a commit of the *type* introduces a new feature to the codebase (this correlates with [`MINOR`](http://semver.org/#summary) in Semantic Versioning).`feat`
3. **BREAKING CHANGE:**  a commit that has a footer , or appends a after the type/scope, introduces a breaking API change (correlating with [`MAJOR`](http://semver.org/#summary) in Semantic Versioning). A BREAKING CHANGE can be part of commits of any *type*.`BREAKING CHANGE:``!`
4. *types* other than and are allowed, for example [@commitlint/config-conventional](https://github.com/conventional-changelog/commitlint/tree/master/%40commitlint/config-conventional) (based on the [Angular convention](https://github.com/angular/angular/blob/22b96b9/CONTRIBUTING.md#-commit-message-guidelines)) recommends , , , , , , , , and others.`fix:``feat:``build:``chore:``ci:``docs:``style:``refactor:``perf:``test:`
5. *footers* other than may be provided and follow a convention similar to [git trailer format](https://git-scm.com/docs/git-interpret-trailers).`BREAKING CHANGE: <description>`

Additional types are not mandated by the Conventional Commits specification, and have no implicit effect in Semantic Versioning (unless they include a BREAKING CHANGE). A scope may be provided to a commit's type, to provide additional contextual information and is contained within parenthesis, e.g., .`feat(parser): add ability to parse arrays`

## Examples

### Commit message with description and breaking change footer

```
feat: allow provided config object to extend other configs

BREAKING CHANGE: `extends` key in config file is now used for extending other config files
```

### Commit message with to draw attention to breaking change`!`

```
feat!: send an email to the customer when a product is shipped
```

### Commit message with scope and to draw attention to breaking change`!`

```
feat(api)!: send an email to the customer when a product is shipped
```

### Commit message with both and BREAKING CHANGE footer`!`

```
chore!: drop support for Node 6

BREAKING CHANGE: use JavaScript features not available in Node 6.
```

### Commit message with no body

```
docs: correct spelling of CHANGELOG
```

### Commit message with scope

```
feat(lang): add Polish language
```

### Commit message with multi-paragraph body and multiple footers

```
fix: prevent racing of requests

Introduce a request id and a reference to latest request. Dismiss
incoming responses other than from latest request.

Remove timeouts which were used to mitigate the racing issue but are
obsolete now.

Reviewed-by: Z
Refs: #123
```

## Specification

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://www.ietf.org/rfc/rfc2119.txt).

1. Commits MUST be prefixed with a type, which consists of a noun, , , etc., followed by the OPTIONAL scope, OPTIONAL , and REQUIRED terminal colon and space.`feat``fix``!`
2. The type MUST be used when a commit adds a new feature to your application or library.`feat`
3. The type MUST be used when a commit represents a bug fix for your application.`fix`
4. A scope MAY be provided after a type. A scope MUST consist of a noun describing a section of the codebase surrounded by parenthesis, e.g., `fix(parser):`
5. A description MUST immediately follow the colon and space after the type/scope prefix. The description is a short summary of the code changes, e.g., *fix: array parsing issue when multiple spaces were contained in string*.
6. A longer commit body MAY be provided after the short description, providing additional contextual information about the code changes. The body MUST begin one blank line after the description.
7. A commit body is free-form and MAY consist of any number of newline separated paragraphs.
8. One or more footers MAY be provided one blank line after the body. Each footer MUST consist of a word token, followed by either a or separator, followed by a string value (this is inspired by the [git trailer convention](https://git-scm.com/docs/git-interpret-trailers)).`:<space>``<space>#`
9. A footer's token MUST use in place of whitespace characters, e.g., (this helps differentiate the footer section from a multi-paragraph body). An exception is made for , which MAY also be used as a token.`-``Acked-by``BREAKING CHANGE`
10. A footer's value MAY contain spaces and newlines, and parsing MUST terminate when the next valid footer token/separator pair is observed.
11. Breaking changes MUST be indicated in the type/scope prefix of a commit, or as an entry in the footer.
12. If included as a footer, a breaking change MUST consist of the uppercase text BREAKING CHANGE, followed by a colon, space, and description, e.g., *BREAKING CHANGE: environment variables now take precedence over config files*.
13. If included in the type/scope prefix, breaking changes MUST be indicated by a immediately before the . If is used, MAY be omitted from the footer section, and the commit description SHALL be used to describe the breaking change.`!``:``!``BREAKING CHANGE:`
14. Types other than and MAY be used in your commit messages, e.g., *docs: update ref docs.*`feat``fix`
15. The units of information that make up Conventional Commits MUST NOT be treated as case sensitive by implementors, with the exception of BREAKING CHANGE which MUST be uppercase.
16. BREAKING-CHANGE MUST be synonymous with BREAKING CHANGE, when used as a token in a footer.

## Why Use Conventional Commits

- Automatically generating CHANGELOGs.
- Automatically determining a semantic version bump (based on the types of commits landed).
- Communicating the nature of changes to teammates, the public, and other stakeholders.
- Triggering build and publish processes.
- Making it easier for people to contribute to your projects, by allowing them to explore a more structured commit history.