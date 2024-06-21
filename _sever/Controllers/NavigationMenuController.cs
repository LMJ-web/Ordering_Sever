using _sever.EF_Core.NavigationMenu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.IdentityModel.Tokens;
using System.Transactions;

namespace _sever.Controllers
{
    [ApiController]
    [Route("[Controller]/[Action]")]
    public class NavigationMenuController: ControllerBase
    {
        private readonly NavigationDbContext navigationDbContext;
        private readonly CompletNode completNode;
        public NavigationMenuController(NavigationDbContext navigationDbContext,CompletNode completNode) { 
            this.navigationDbContext = navigationDbContext;
            this.completNode = completNode;
        }
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddNavigationNode(NavigationRecord navigationVo)
        {
            NavigationRecord navigationRecordInDb = navigationDbContext.navigationRecords.SingleOrDefault(entity => entity.Id == navigationVo.ParentId);
            if (navigationRecordInDb == null) { return NotFound("父节点不存在！"); }
            if (navigationRecordInDb.NavigationName != navigationVo.ParentName) { return BadRequest("父节点错误！"); }
            EntityEntry< NavigationRecord > e = await navigationDbContext.navigationRecords.AddAsync(navigationVo);
            Console.WriteLine(e.State.ToString());
            Console.WriteLine(!e.State.ToString().Equals("Added"));
            if (!e.State.ToString().Equals("Added")){ return BadRequest("添加失败！"); }
            await navigationDbContext.SaveChangesAsync();
            return Ok("添加成功！");
        }
        [HttpGet]
        [Authorize(Roles = "admin,common")]
        public IActionResult GetNavigationMenu()
        {
            NavigationRecord rootRecord = navigationDbContext.navigationRecords.Single(entity => entity.Id == 1);
            NavigationNode rootNode = new NavigationNode(rootRecord.Id, rootRecord.NavigationName, rootRecord.ParentId, rootRecord.ParentName, rootRecord.PriorityLevel, new List<NavigationNode>(), rootRecord.Type, rootRecord.Path);
            NavigationNode rootNodeWithChildren = completNode.CompletTheChildrenOfNode(rootNode);
            return Ok(rootNodeWithChildren);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteNavigationNode(int id) {
            NavigationRecord record = navigationDbContext.navigationRecords.Single(entity => entity.Id == id);
            using (TransactionScope tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                EntityEntry<NavigationRecord> removeResult = navigationDbContext.navigationRecords.Remove(record);
                if (record.Type == 0){ 
                    IEnumerable<NavigationRecord> childrenRecords = navigationDbContext.navigationRecords.Where(entity => entity.ParentId == id).ToArray();
                    if (!childrenRecords.IsNullOrEmpty())
                    {
                        foreach (NavigationRecord childRecord in childrenRecords)
                        {
                            navigationDbContext.navigationRecords.Remove(childRecord);
                        }
                    }
                }
                await navigationDbContext.SaveChangesAsync();
                tx.Complete();
                Console.WriteLine(removeResult.State.ToString());
                if (removeResult.State.ToString().Equals("Detached"))
                {
                    return Ok($"{record.NavigationName},删除成功！");
                }
                return BadRequest($"{record.NavigationName},删除失败！");
            }
            
        }
        [HttpPatch]
        public IActionResult PatchNavigation(NavigationRecord navigationRecord)
        {
            NavigationRecord navigationRecordInDb = navigationDbContext.navigationRecords.SingleOrDefault(e => e.Id == navigationRecord.Id);
            if (navigationRecordInDb == null) return BadRequest("节点不存在！");
            navigationRecordInDb.NavigationName = navigationRecord.NavigationName.ToString();
            navigationRecordInDb.ParentId = navigationRecord.ParentId;
            navigationRecordInDb.ParentName = navigationRecord.ParentName.ToString();
            navigationRecordInDb.PriorityLevel = navigationRecord.PriorityLevel;
            navigationRecordInDb.Type = navigationRecord.Type;
            navigationRecordInDb.Path = navigationRecord.Path.ToString();

            
            navigationDbContext.SaveChanges();
            return Ok("修改成功！");

        }
    }
}
