using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Notebook
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        [HttpPost]
        [ActionName("getitem")]
        public IActionResult Post([FromBody] ItemQueryClient qc)
        {
            ItemQuery q;

            //Validate session token
            if(qc.Validate(out q))
            {
                //Return multiple items
                if (q.multiple) return Ok(Fetch.GetItems(q));

                else return Ok(Items.GetItem(q));
            }
            else
            {
                //Invalid session token
                return StatusCode(403);
            }
        }

        [HttpPost]
        [ActionName("putitem")]
        public IActionResult Post([FromBody] ItemCommit i)
        {            
            if(i.append)
            {
                int user_id;

                if (!Auth.ValidateSession(i.session_id, out user_id, out _))
                    return Ok(new JSONResult("Error: Invalid Session"));

                //Check if editing is enabled for this user
                var t = Lock.GetLockout(i.item.id);

                if (t == null || t.Item1 != user_id)
                    return Ok(new JSONResult(true));

                OwnershipType o = Auth.CheckOwnership(user_id, i.item.id);

                if (o == OwnershipType.OwnerCreator || o == OwnershipType.SharedItem || o == OwnershipType.SharedCollection)
                {
                    Event.Log(ApplicationEvent.Item_Append, $"session_id:{i.session_id},item_id:{i.item.id}");
                    var result = Items.UpdateItem(i.item, i.append, o, user_id);
                    if(i.close) Lock.Unlock(i.item.id);
                    return Ok(result);
                }
                else
                {
                    return Ok(new JSONResult("Error: You don't have permission to change this item"));
                }
            }
            else
            {
                int user_id;
                if(Auth.ValidateSession(i.session_id, out user_id, out _))
                {
                    Event.Log(ApplicationEvent.Item_Add, $"user_id:{user_id}");
                    return Ok(Items.UpdateItem(i.item, false, OwnershipType.NotOwned, user_id));
                }
                else
                {
                    return Ok(new JSONResult("Error: Invalid Session"));
                }
            }
        }

        // DELETE: api/ApiWithActions/5
        [HttpPost]
        [ActionName("itemaction")]
        public IActionResult Post([FromBody] ItemAction i)
        {
            return Ok(Items.ItemModify(i));
        }
    }
}
