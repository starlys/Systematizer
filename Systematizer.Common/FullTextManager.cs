using Systematizer.Common.PersistentModel;

namespace Systematizer.Common;

/// <summary>
/// Helper for full text index in Word table. To use, populate the ...ToIndex members, then call WriteIndex.
/// </summary>
class FullTextManager
{
    public IndexableWordSet TitleToIndex = new();
    public IndexableWordSet DetailsToIndex = new();

    /// <summary>
    /// Writes any changes to index, saves changes
    /// </summary>
    public void WriteIndex(SystematizerContext db, short kind, long parentId)
    {
        //get existing index records and words that should be indexed
        var records = db.Word.Where(w => w.Kind == kind && w.ParentId == parentId).ToList();
        var shouldIndexTitle = TitleToIndex.GetIndexable();
        var shouldIndexDetail = DetailsToIndex.GetIndexable();

        //eliminate entries from both lists where the match exists (already indexed)
        //and optionally modify the detail level in the record
        for (int ridx = records.Count - 1; ridx >= 0; --ridx)
        {
            var record = records[ridx];
            bool foundInTitle = false, foundInDetail = false;
            if (shouldIndexTitle.Contains(record.Word8)) { foundInTitle = true; shouldIndexTitle.Remove(record.Word8); }
            if (shouldIndexDetail.Contains(record.Word8)) { foundInDetail = true; shouldIndexDetail.Remove(record.Word8); }

            //switch from detail-only to title or vice versa
            if (record.IsDetail != 0 && foundInTitle)
                record.IsDetail = 0;
            else if (record.IsDetail == 0 && foundInDetail && !foundInTitle)
                record.IsDetail = 1;

            if (foundInTitle || foundInDetail)
                records.RemoveAt(ridx);
        }

        //for new words, create records for them
        foreach (string word in shouldIndexTitle)
        {
            shouldIndexDetail.Remove(word);
            var record = CreateOrReuseRecord(db, records);
            record.IsDetail = 0;
            record.Kind = kind;
            record.ParentId = parentId;
            record.Word8 = word;
        }
        foreach (string word in shouldIndexDetail)
        {
            var record = CreateOrReuseRecord(db, records);
            record.IsDetail = 1;
            record.Kind = kind;
            record.ParentId = parentId;
            record.Word8 = word;
        }

        //delete any remaining records from old index
        db.Word.RemoveRange(records);

        db.SaveChanges();
    }

    /// <summary>
    /// If list has elements, return one and remove, else create one and add to context
    /// </summary>
    static Word CreateOrReuseRecord(SystematizerContext db, List<Word> records)
    {
        int lastRecIdx = records.Count - 1;
        if (lastRecIdx >= 0)
        {
            var record = records[lastRecIdx];
            records.RemoveAt(lastRecIdx);
            return record;
        }
        else
        {
            var record = new Word();
            db.Word.Add(record);
            return record;
        }
    }
}
