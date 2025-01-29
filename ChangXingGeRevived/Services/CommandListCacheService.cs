using ChangXingGeRevived.Databases;
using ChangXingGeRevived.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ChangXingGeRevived.Services;

public class CommandListCacheService
{
    private readonly BotDbContext _db;
    private static ConcurrentDictionary<string, CommandRecord> _commandRecordCache = new();
    public CommandListCacheService(BotDbContext botDbContext)
    {
        _db = botDbContext;
    }
    public void FetchAllFromDatabase()
    {
        _commandRecordCache.Clear();
        foreach (var commandRecord in _db.CommandRecords)
        {
            if (!_commandRecordCache.TryAdd(commandRecord.Keyword, commandRecord))
            {
                throw new Exception("Update command record cache failed");
            }
        }
    }
    public bool TryGetCommand(string keyword, [MaybeNullWhen(false)] out CommandRecord record)
    {
        if (_commandRecordCache.IsEmpty)
        {
            FetchAllFromDatabase();
        }
        return _commandRecordCache.TryGetValue(keyword, out record);
    }
    public void UpdateCommandState(CommandRecord record)
    {
        if (_commandRecordCache.IsEmpty)
        {
            FetchAllFromDatabase();
        }
        if (!_commandRecordCache.TryUpdate(record.Keyword, record, _commandRecordCache[record.Keyword]))
        {
            throw new Exception("Update command record cache failed");
        }
        _db.CommandRecords.Update(record);
        _db.SaveChanges();
    }
}
