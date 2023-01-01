using Systematizer.Common.PersistentModel;

namespace Systematizer.Common;

/// <summary>
/// Person from database with extra parsed info. 
/// </summary>
public class ExtPerson
{
    /// <summary>
    /// The person from the database model
    /// </summary>
    public Person Person;

    public List<LinkRecord> Links;

    public long[] SelectedCatIds;

    /// <param name="links">optional (see DBUtil.LoadLinksFor)</param>
    /// <param name="selectedCatIds">optional</param>
    public ExtPerson(Person person, List<LinkRecord> links, long[] selectedCatIds)
    {
        Person = person;
        Links = links ?? new List<LinkRecord>();
        SelectedCatIds = selectedCatIds ?? Array.Empty<long>();
    }
}
