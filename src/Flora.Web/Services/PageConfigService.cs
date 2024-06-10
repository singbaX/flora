using AiCodo.Data;
using AiCodo.Flow;
using System.Collections;
using System.Data.Common;
using System.Diagnostics;

namespace Flora.Web.Services
{
    public class PageConfigService
    {
        [ServiceMethod("GetPageConfigFile")]
        public static IFileResult GetPageConfigFile(string module, string page, string name)
        {
            var file = "";
            if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                file = System.IO.Path.Combine(ApplicationConfig.LocalConfigFolder, $"Pages/{module}/{page}/{name}");
            }
            else if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                file = System.IO.Path.Combine(ApplicationConfig.LocalConfigFolder, $"Pages/{module}/{page}/{name}");
            }
            else
            {
                file = System.IO.Path.Combine(ApplicationConfig.LocalConfigFolder, $"Pages/{module}/{page}/{name}.xml");
            }
            return new FileResult(file);
        }

        [ServiceMethod("SavePageAuthValues")]
        public static bool SavePageAuthValues(int currentUserID, int pageID, IEnumerable values)
        {
            lock (Locks.GetLock($"sys_page_auth.Page_{pageID}"))
            {
                var items = SqlService.ExecuteQuery<DynamicEntity>("sys_page_auth.SelectByPageID", "PageID", pageID).ToList();
                var newItems = values.Cast<DynamicEntity>().Select(v => new
                {
                    AuthValue = v.GetInt32("AuthValue"),
                    DisplayName = v.GetString("DisplayName"),
                });

                var insertItems = new List<DynamicEntity>();
                var updateItems = new List<DynamicEntity>();
                var now = DateTime.Now;

                foreach (var item in newItems)
                {
                    var oldItem = items.FirstOrDefault(f => f.GetInt32("AuthValue") == item.AuthValue);
                    if (oldItem != null)
                    {
                        if (oldItem.GetString("DisplayName") == item.DisplayName && oldItem.GetBool("IsValid"))
                        {
                            continue;
                        }

                        oldItem.SetValue("DisplayName", item.DisplayName);
                        oldItem.SetValue("IsValid", true);
                        oldItem.SetValue("UpdateUser", currentUserID);
                        oldItem.SetValue("UpdateTime", now);
                        updateItems.Add(oldItem);

                        items.Remove(oldItem);
                    }
                    else
                    {
                        oldItem = new DynamicEntity
                            ("PageID", pageID
                              , "AuthValue", item.AuthValue
                              , "DisplayName", item.DisplayName
                              , "IsValid", 1
                              , "CreateUser", currentUserID
                              , "CreateTime", now
                              , "UpdateUser", currentUserID
                              , "UpdateTime", now
                            );
                        insertItems.Add(oldItem);
                    }
                }
                items.ForEach(v =>
                {
                    v.SetValue("IsValid", true);
                    v.SetValue("UpdateUser", currentUserID);
                    v.SetValue("UpdateTime", now);
                });
                var removeItems = items;
                try
                {
                    var insertSql = SqlData.Current.GetSqlItem($"sys_page_auth.Insert");
                    if (insertSql == null)
                    {
                        throw new Exception("sys_page_auth.Insert 没有设置");
                    }
                    var updateSql = SqlData.Current.GetSqlItem($"sys_page_auth.Update");
                    if (insertSql == null)
                    {
                        throw new Exception("sys_page_auth.Update 没有设置");
                    }

                    using (DbConnection db = SqlData.Current.OpenConnection("AiCodo"))
                    {
                        var trans = db.BeginTransaction();
                        try
                        {
                            foreach (var item in removeItems)
                            {
                                db.ExecuteNoneQuery(trans, updateSql, item.ToNameValues());
                            }

                            foreach (var item in updateItems)
                            {
                                db.ExecuteNoneQuery(trans, updateSql, item.ToNameValues());
                            }

                            foreach (var item in insertItems)
                            {
                                db.ExecuteScalar(trans, insertSql, item.ToNameValues());
                            }
                            trans.Commit();
                        }
                        catch (Exception)
                        {
                            trans.Rollback();
                        }
                        finally
                        {
                            trans.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.WriteErrorLog();
                    return false;
                }
            }



            return true;
        }
    }
}
