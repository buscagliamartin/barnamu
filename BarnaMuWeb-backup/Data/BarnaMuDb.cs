// BarnaMu Web - BarnaMuDb.cs
// Data access layer. Uses Npgsql + Dapper directly against the OpenMU PostgreSQL database.
// We do NOT take a dependency on OpenMU's persistence assemblies on purpose: the website
// only needs the well-known schema of data."Account" and data."Character" plus BCrypt for
// passwords. Keeping the website self-contained means it builds cleanly with three NuGet
// packages and can be moved to its own VPS in the future without dragging OpenMU code with it.

using System.Data;
using BCrypt.Net;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BarnaMu.Web.Data;

public class BarnaMuDb
{
    private readonly BarnaMuOptions _options;
    private readonly ILogger<BarnaMuDb> _logger;

    public BarnaMuDb(IOptions<BarnaMuOptions> options, ILogger<BarnaMuDb> logger)
    {
        this._options = options.Value;
        this._logger = logger;
    }

    private IDbConnection Open()
    {
        var conn = new NpgsqlConnection(this._options.ConnectionString);
        conn.Open();
        return conn;
    }

    // -------- Account creation --------

    public async Task<bool> AccountExistsAsync(string loginName)
    {
        using var conn = this.Open();
        var existing = await conn.ExecuteScalarAsync<int?>(
            """SELECT 1 FROM data."Account" WHERE LOWER("LoginName") = LOWER(@LoginName) LIMIT 1""",
            new { LoginName = loginName });
        return existing.HasValue;
    }

    /// <summary>
    /// Creates a new account. Returns true on success, false if the login is already taken.
    /// The password is hashed with BCrypt using the same library version OpenMU uses
    /// (BCrypt.Net-Next 4.0.3) so the account is immediately loginable in-game.
    /// </summary>
    public async Task<AccountCreateResult> CreateAccountAsync(
        string loginName,
        string password,
        string securityCode,
        string? email)
    {
        // Defensive double-check (the unique index in Postgres is the real guarantee).
        if (await this.AccountExistsAsync(loginName))
        {
            return AccountCreateResult.AlreadyExists;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        using var conn = this.Open();
        try
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO data."Account"
                    ("Id", "LoginName", "PasswordHash", "SecurityCode", "EMail",
                     "State", "RegistrationDate", "IsVaultExtended", "IsTemplate",
                     "TimeZone", "VaultPassword", "LanguageIsoCode")
                VALUES
                    (@Id, @LoginName, @PasswordHash, @SecurityCode, @EMail,
                     @State, @RegistrationDate, @IsVaultExtended, @IsTemplate,
                     @TimeZone, @VaultPassword, @LanguageIsoCode)
                """,
                new
                {
                    Id = Guid.NewGuid(),
                    LoginName = loginName,
                    PasswordHash = passwordHash,
                    SecurityCode = securityCode,
                    EMail = email ?? string.Empty,
                    State = 0, // AccountState.Normal — see DataModel/Entities/Account.cs
                    RegistrationDate = DateTime.UtcNow,
                    IsVaultExtended = false,
                    IsTemplate = false,
                    TimeZone = (short)0,
                    VaultPassword = string.Empty, // NOT NULL column in the OpenMU schema
                    LanguageIsoCode = "en",
                });
        }
        catch (PostgresException pgex) when (pgex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Race between AccountExistsAsync and INSERT — the unique index caught it.
            return AccountCreateResult.AlreadyExists;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to create account [{LoginName}].", loginName);
            return AccountCreateResult.Error;
        }

        return AccountCreateResult.Created;
    }

    // -------- Stats / status --------

    public async Task<long> GetTotalAccountsAsync()
    {
        using var conn = this.Open();
        return await conn.ExecuteScalarAsync<long>("""SELECT COUNT(*) FROM data."Account" WHERE "IsTemplate" = false""");
    }

    public async Task<long> GetTotalCharactersAsync()
    {
        using var conn = this.Open();
        return await conn.ExecuteScalarAsync<long>(@"SELECT COUNT(*) FROM data.""Character""");
    }

    public async Task<long> GetNewAccountsLastDaysAsync(int days)
    {
        using var conn = this.Open();
        return await conn.ExecuteScalarAsync<long>(
            """SELECT COUNT(*) FROM data."Account" WHERE "IsTemplate" = false AND "RegistrationDate" >= @Since""",
            new { Since = DateTime.UtcNow.AddDays(-days) });
    }

    /// <summary>
    /// Returns accounts whose State != 0. OpenMU sets State=0 (Normal) when offline
    /// and changes it when a session is active, so this is a reliable online counter.
    /// </summary>
    public async Task<long> GetOnlinePlayersAsync()
    {
        using var conn = this.Open();
        return await conn.ExecuteScalarAsync<long>(
            """SELECT COUNT(*) FROM data."Account" WHERE "State" != 0 AND "IsTemplate" = false""");
    }

    // -------- Rankings --------
    //
    // Las definiciones de stats viven en config."AttributeDefinition" con Guids estables
    // hardcoded en MUnique.OpenMU.GameLogic.Attributes.Stats.cs. Son los mismos en cualquier
    // OpenMU, así que usamos los Guids directamente.
    //   Level          560931AD-0901-4342-B7F4-FD2E2FCC0563
    //   MasterLevel    70CD8C10-391A-4C51-9AA4-A854600E3A9F
    //   Resets         89A891A7-F9F9-4AB5-AF36-12056E53A5F7
    private const string LevelAttributeId      = "560931AD-0901-4342-B7F4-FD2E2FCC0563";
    private const string MasterLevelAttributeId = "70CD8C10-391A-4C51-9AA4-A854600E3A9F";
    private const string ResetsAttributeId     = "89A891A7-F9F9-4AB5-AF36-12056E53A5F7";

    /// <summary>Top characters by reset count (then by experience as tiebreaker).</summary>
    public async Task<IReadOnlyList<RankingRow>> GetTopByResetsAsync(int limit = 50)
    {
        using var conn = this.Open();
        var rows = await conn.QueryAsync<RankingRow>(
            $"""
            SELECT c."Name" AS "Name",
                   CAST(sa."Value" AS integer) AS "Resets",
                   COALESCE(CAST(lvl."Value" AS integer), 0) AS "Level",
                   COALESCE(CAST(ml."Value"  AS integer), 0) AS "MasterLevel",
                   c."CreateDate"
            FROM data."Character" c
            JOIN data."StatAttribute" sa
              ON sa."CharacterId" = c."Id"
             AND sa."DefinitionId" = '{ResetsAttributeId}'
            LEFT JOIN data."StatAttribute" lvl
              ON lvl."CharacterId" = c."Id"
             AND lvl."DefinitionId" = '{LevelAttributeId}'
            LEFT JOIN data."StatAttribute" ml
              ON ml."CharacterId" = c."Id"
             AND ml."DefinitionId" = '{MasterLevelAttributeId}'
            WHERE sa."Value" > 0
            ORDER BY sa."Value" DESC, c."Experience" DESC
            LIMIT @Limit
            """,
            new { Limit = limit });
        return rows.AsList();
    }

    /// <summary>Top characters by current level (with master level shown alongside).</summary>
    public async Task<IReadOnlyList<RankingRow>> GetTopByLevelAsync(int limit = 50)
    {
        using var conn = this.Open();
        var rows = await conn.QueryAsync<RankingRow>(
            $"""
            SELECT c."Name" AS "Name",
                   COALESCE(CAST(lvl."Value" AS integer), 0) AS "Level",
                   COALESCE(CAST(ml."Value"  AS integer), 0) AS "MasterLevel",
                   COALESCE(CAST(rst."Value" AS integer), 0) AS "Resets",
                   c."CreateDate"
            FROM data."Character" c
            LEFT JOIN data."StatAttribute" lvl
              ON lvl."CharacterId" = c."Id"
             AND lvl."DefinitionId" = '{LevelAttributeId}'
            LEFT JOIN data."StatAttribute" ml
              ON ml."CharacterId" = c."Id"
             AND ml."DefinitionId" = '{MasterLevelAttributeId}'
            LEFT JOIN data."StatAttribute" rst
              ON rst."CharacterId" = c."Id"
             AND rst."DefinitionId" = '{ResetsAttributeId}'
            ORDER BY COALESCE(lvl."Value", 0) DESC,
                     COALESCE(ml."Value",  0) DESC,
                     c."Experience" DESC
            LIMIT @Limit
            """,
            new { Limit = limit });
        return rows.AsList();
    }

    /// <summary>Top guilds by Score then by member count.</summary>
    public async Task<IReadOnlyList<GuildRankingRow>> GetTopGuildsAsync(int limit = 50)
    {
        using var conn = this.Open();
        var rows = await conn.QueryAsync<GuildRankingRow>(
            @"SELECT g.""Name"" AS ""Name"",
                     g.""Score"" AS ""Score"",
                     COUNT(gm.""Id"") AS ""MemberCount""
              FROM guild.""Guild"" g
              LEFT JOIN guild.""GuildMember"" gm ON gm.""GuildId"" = g.""Id""
              GROUP BY g.""Id"", g.""Name"", g.""Score""
              ORDER BY g.""Score"" DESC, COUNT(gm.""Id"") DESC, g.""Name"" ASC
              LIMIT @Limit",
            new { Limit = limit });
        return rows.AsList();
    }

    // -------- Account panel --------

    private sealed class AccountLoginRow
    {
        public Guid Id { get; set; }
        public string LoginName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime? VipExpirationDate { get; set; }
    }

    /// <summary>
    /// Validates credentials and returns account info on success, null on failure.
    /// Timing-safe: BCrypt.Verify always runs (no early-out on username not found).
    /// </summary>
    public async Task<AccountInfo?> ValidateLoginAsync(string loginName, string password)
    {
        using var conn = this.Open();
        var row = await conn.QueryFirstOrDefaultAsync<AccountLoginRow>(
            """
            SELECT "Id", "LoginName", "PasswordHash", "VipExpirationDate"
            FROM data."Account"
            WHERE LOWER("LoginName") = LOWER(@LoginName) AND "IsTemplate" = false
            LIMIT 1
            """,
            new { LoginName = loginName });

        if (row is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, row.PasswordHash)) return null;

        return new AccountInfo
        {
            Id                = row.Id,
            LoginName         = row.LoginName,
            VipExpirationDate = row.VipExpirationDate,
        };
    }

    /// <summary>Returns all characters for the given account, ordered by resets desc.</summary>
    public async Task<IReadOnlyList<AccountCharacter>> GetAccountCharactersAsync(Guid accountId)
    {
        using var conn = this.Open();
        var rows = await conn.QueryAsync<AccountCharacter>(
            $"""
            SELECT c."Name",
                   c."CreateDate",
                   cc."Name"                                  AS "ClassName",
                   COALESCE(CAST(lvl."Value" AS integer), 0)  AS "Level",
                   COALESCE(CAST(ml."Value"  AS integer), 0)  AS "MasterLevel",
                   COALESCE(CAST(rst."Value" AS integer), 0)  AS "Resets",
                   g."Name"                                   AS "GuildName"
            FROM data."Character" c
            LEFT JOIN config."CharacterClass" cc ON cc."Id" = c."CharacterClassId"
            LEFT JOIN guild."GuildMember" gm     ON gm."Id" = c."Id"
            LEFT JOIN guild."Guild" g            ON g."Id" = gm."GuildId"
            LEFT JOIN data."StatAttribute" lvl   ON lvl."CharacterId" = c."Id"
                                                 AND lvl."DefinitionId" = '{LevelAttributeId}'
            LEFT JOIN data."StatAttribute" ml    ON ml."CharacterId" = c."Id"
                                                 AND ml."DefinitionId" = '{MasterLevelAttributeId}'
            LEFT JOIN data."StatAttribute" rst   ON rst."CharacterId" = c."Id"
                                                 AND rst."DefinitionId" = '{ResetsAttributeId}'
            WHERE c."AccountId" = @AccountId
            ORDER BY COALESCE(rst."Value", 0) DESC, COALESCE(lvl."Value", 0) DESC
            """,
            new { AccountId = accountId });
        return rows.AsList();
    }

    /// <summary>Fetches fresh account info (used by the panel to get up-to-date VIP date).</summary>
    public async Task<AccountInfo?> GetAccountInfoAsync(Guid accountId)
    {
        using var conn = this.Open();
        return await conn.QueryFirstOrDefaultAsync<AccountInfo>(
            """
            SELECT "Id", "LoginName", "VipExpirationDate"
            FROM data."Account"
            WHERE "Id" = @Id AND "IsTemplate" = false
            LIMIT 1
            """,
            new { Id = accountId });
    }

    /// <summary>Updates the BCrypt password hash for an account.</summary>
    public async Task ChangePasswordAsync(Guid accountId, string newPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        using var conn = this.Open();
        await conn.ExecuteAsync(
            """UPDATE data."Account" SET "PasswordHash" = @Hash WHERE "Id" = @Id""",
            new { Hash = hash, Id = accountId });
    }

    /// <summary>Full profile for a single character, including class, guild and stats.</summary>
    public async Task<CharacterProfile?> GetCharacterProfileAsync(string name)
    {
        using var conn = this.Open();
        return await conn.QueryFirstOrDefaultAsync<CharacterProfile>(
            $"""
            SELECT c."Name",
                   c."CreateDate",
                   cc."Name"                                  AS "ClassName",
                   COALESCE(CAST(lvl."Value" AS integer), 0)  AS "Level",
                   COALESCE(CAST(ml."Value"  AS integer), 0)  AS "MasterLevel",
                   COALESCE(CAST(rst."Value" AS integer), 0)  AS "Resets",
                   g."Name"                                   AS "GuildName"
            FROM data."Character" c
            LEFT JOIN config."CharacterClass" cc   ON cc."Id" = c."CharacterClassId"
            LEFT JOIN guild."GuildMember" gm       ON gm."Id" = c."Id"
            LEFT JOIN guild."Guild" g              ON g."Id" = gm."GuildId"
            LEFT JOIN data."StatAttribute" lvl     ON lvl."CharacterId" = c."Id"
                                                   AND lvl."DefinitionId" = '{LevelAttributeId}'
            LEFT JOIN data."StatAttribute" ml      ON ml."CharacterId" = c."Id"
                                                   AND ml."DefinitionId" = '{MasterLevelAttributeId}'
            LEFT JOIN data."StatAttribute" rst     ON rst."CharacterId" = c."Id"
                                                   AND rst."DefinitionId" = '{ResetsAttributeId}'
            WHERE LOWER(c."Name") = LOWER(@Name)
            LIMIT 1
            """,
            new { Name = name });
    }

    // -------- Bug reports (own schema, never collides with OpenMU migrations) --------

    public async Task EnsureWebSchemaAsync()
    {
        try
        {
            using var conn = this.Open();
            await conn.ExecuteAsync(
                """
                CREATE SCHEMA IF NOT EXISTS web;

                CREATE TABLE IF NOT EXISTS web."BugReport" (
                    "Id"         bigserial PRIMARY KEY,
                    "Reporter"   text       NOT NULL,
                    "Email"      text       NULL,
                    "Title"      text       NOT NULL,
                    "Body"       text       NOT NULL,
                    "CreatedAt"  timestamptz NOT NULL DEFAULT now(),
                    "Ip"         text       NULL,
                    "UserAgent"  text       NULL,
                    "Status"     text       NOT NULL DEFAULT 'new'
                );

                CREATE INDEX IF NOT EXISTS "IX_BugReport_CreatedAt"
                    ON web."BugReport" ("CreatedAt" DESC);
                """);
            this._logger.LogInformation("Web schema and BugReport table ensured.");
        }
        catch (Exception ex)
        {
            // If this fails, the registration / rankings pages still work; only bug reports break.
            this._logger.LogError(ex, "Could not ensure the web schema. Bug reports will not be saved.");
        }
    }

    public async Task InsertBugReportAsync(string reporter, string? email, string title, string body, string? ip, string? userAgent)
    {
        using var conn = this.Open();
        await conn.ExecuteAsync(
            """
            INSERT INTO web."BugReport" ("Reporter", "Email", "Title", "Body", "Ip", "UserAgent")
            VALUES (@Reporter, @Email, @Title, @Body, @Ip, @UserAgent)
            """,
            new { Reporter = reporter, Email = email, Title = title, Body = body, Ip = ip, UserAgent = userAgent });
    }
}

public enum AccountCreateResult
{
    Created,
    AlreadyExists,
    Error,
}

public class RankingRow
{
    public string Name { get; set; } = string.Empty;
    public int Resets { get; set; }
    public int Level { get; set; }
    public int MasterLevel { get; set; }
    public DateTime CreateDate { get; set; }
}

public class GuildRankingRow
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public long MemberCount { get; set; }
}

public class CharacterProfile
{
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public int Level { get; set; }
    public int MasterLevel { get; set; }
    public int Resets { get; set; }
    public string? GuildName { get; set; }
}

public class AccountInfo
{
    public Guid Id { get; set; }
    public string LoginName { get; set; } = string.Empty;
    public DateTime? VipExpirationDate { get; set; }

    public bool IsVip => VipExpirationDate.HasValue && VipExpirationDate.Value > DateTime.UtcNow;
    public TimeSpan? VipRemaining => IsVip ? VipExpirationDate!.Value - DateTime.UtcNow : null;
}

public class AccountCharacter
{
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public int Level { get; set; }
    public int MasterLevel { get; set; }
    public int Resets { get; set; }
    public string? GuildName { get; set; }
}
