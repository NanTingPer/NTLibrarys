using System.Threading.Tasks;
namespace AgentNexus.Tooling;

public interface IToolData<TModel>
    where TModel : class, new()
{
    Task<TModel> GetData();
}