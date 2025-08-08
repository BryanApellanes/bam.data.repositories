
namespace Bam.Data.Repositories;

public class ManyToMany<TL,TR> : KeyedAuditRepoData where TL : RepoData where TR : RepoData
{
    // TODO: explore this later
    /*public string LeftType { get; set; }
    public string RightType { get; set; }
    public virtual TL Left { get; set; }
    public virtual TR Right { get; set; }

    public virtual IEnumerable<TR> GetChildrenOfLeft()*/
}