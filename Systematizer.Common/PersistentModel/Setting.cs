namespace Systematizer.Common.PersistentModel;

public partial class Setting : BaseTable
{
    public string Custom1Label { get; set; }
    public string Custom2Label { get; set; }
    public string Custom3Label { get; set; }
    public string Custom4Label { get; set; }
    public string Custom5Label { get; set; }
    public string ChunkInfo { get; set; }
    public short AllowTasks { get; set; }
}
