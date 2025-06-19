using DynamicFormsApp.Server.Data;
using DynamicFormsApp.Shared.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynamicFormsApp.Server.Services
{
    public class DynamicFormService
    {
        private readonly AppDbContext _db;

        public DynamicFormService(AppDbContext db)
        {
            _db = db;
        }

        private string SanitizeKey(string raw) =>
            Regex.Replace(raw, @"[^\w]", "_");

        public async Task<int> CreateFormAsync(string formName, List<FormField> fields, string createdBy, bool requireLogin, bool notifyOnResponse)
        {
            var form = new Form
            {
                Name = formName,
                CreatedBy = createdBy,
                RequireLogin = requireLogin,
                NotifyOnResponse = notifyOnResponse,
                Fields = fields.Select(f => new FormField
                {
                    Key = SanitizeKey(f.Key),
                    Label = f.Label,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    OptionsJson = f.OptionsJson,
                    Row = f.Row,
                    Column = f.Column
                }).ToList()
            };
            _db.Forms.Add(form);
            await _db.SaveChangesAsync();

            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{form.Id}_{rawName}";
            var sb = new StringBuilder(
                $"CREATE TABLE [{tableName}] (" +
                "ResponseId INT IDENTITY(1,1) PRIMARY KEY, " +
                "CreatedAt DATETIME2 NOT NULL");

            foreach (var fld in form.Fields)
            {
                sb.Append($", [{fld.Key}] {MapToSqlType(fld.FieldType)} {(fld.IsRequired ? "NOT NULL" : "NULL")}");
            }
            sb.Append(");");

            await _db.Database.ExecuteSqlRawAsync(sb.ToString());
            return form.Id;
        }

        public async Task<Form> GetFormAsync(int formId)
        {
            return await _db.Forms
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == formId)
                ?? throw new InvalidOperationException("Form not found");
        }

        public async Task<Form> StoreResponseAsync(int formId, Dictionary<string, object> values)
        {
            var form = await _db.Forms.FindAsync(formId)
                       ?? throw new InvalidOperationException("Form not found");
            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{formId}_{rawName}";

            var cols = string.Join(", ", values.Keys.Select(k => $"[{k}]")) + ", CreatedAt";
            var paramNames = string.Join(", ",
                values.Keys.Select((k, i) => $"@p{i}")
                           .Concat(new[] { "@p_created" }));

            var sql = $"INSERT INTO [{tableName}] ({cols}) VALUES ({paramNames});";

            var sqlParams = new List<SqlParameter>();
            int idx = 0;
            foreach (var kv in values)
            {
                object raw = kv.Value;
                if (raw is JsonElement je)
                {
                    raw = je.ValueKind switch
                    {
                        JsonValueKind.String => je.GetString(),
                        JsonValueKind.Number when je.TryGetInt64(out var l) => l,
                        JsonValueKind.Number when je.TryGetDouble(out var d) => d,
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Array => je.GetRawText(), // Store JSON string for arrays
                        JsonValueKind.Object => je.GetRawText(),
                        JsonValueKind.Null => null,
                        _ => je.GetRawText(),
                    };
                }

                if (raw is List<string> stringList)
                {
                    raw = JsonSerializer.Serialize(stringList);
                }

                sqlParams.Add(new SqlParameter($"@p{idx}", raw ?? DBNull.Value));
                idx++;
            }

            sqlParams.Add(new SqlParameter("@p_created", DateTime.UtcNow));
            await _db.Database.ExecuteSqlRawAsync(sql, sqlParams.ToArray());

            return form;
        }

        public async Task<List<Form>> GetAllFormsAsync()
        {
            return await _db.Forms
                .Include(f => f.Fields)
                .ToListAsync();
        }

        public async Task<List<Form>> GetFormsByUserAsync(string user)
        {
            return await _db.Forms
                .Include(f => f.Fields)
                .Where(f => f.CreatedBy == user)
                .ToListAsync();
        }

        public async Task<List<Dictionary<string, object>>> GetResponsesAsync(int formId)
        {
            var form = await _db.Forms.FindAsync(formId)
                       ?? throw new InvalidOperationException("Form not found");
            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{formId}_{rawName}";

            using var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{tableName}];";
            using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var val = await reader.IsDBNullAsync(i)
                               ? null
                               : reader.GetValue(i);
                    row[name] = val!;
                }
                results.Add(row);
            }
            return results;
        }

        private string MapToSqlType(string fieldType) => fieldType switch
        {
            "number" => "FLOAT",
            "date" => "DATE",
            "time" => "TIME",
            "datetime" => "DATETIME2",
            "file" => "NVARCHAR(MAX)",
            "checkbox" => "NVARCHAR(MAX)",      // Store as JSON array
            "dropdown" => "NVARCHAR(255)",
            "radio" => "NVARCHAR(255)",
            "textarea" => "NVARCHAR(MAX)",
            "grid_radio" => "NVARCHAR(MAX)",    // JSON object
            "grid_checkbox" => "NVARCHAR(MAX)", // JSON object
            "scale" => "INT",
            _ => "NVARCHAR(MAX)"
        };
    }
}
