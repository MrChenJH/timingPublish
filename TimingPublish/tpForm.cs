using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DAL;
using Model;
using System.Net;
using System.IO;
using Model.Redis;
using Newtonsoft.Json;
using System.Configuration;
using log4net.Config;
using log4net;


namespace TimingPublish
{
    public partial class tpForm : Form
    {
        public tpForm()
        {
            InitializeComponent();
        }


        public string PostValue(string Url, string value)
        {
            try
            {
                Encoding myEncoding = Encoding.Default;
                byte[] postBytes = Encoding.UTF8.GetBytes(value);
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(Url);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.ContentLength = postBytes.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(postBytes, 0, postBytes.Length);
                }
                using (WebResponse wr = req.GetResponse())
                {
                    System.IO.Stream respStream = wr.GetResponseStream();
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream, Encoding.Default))
                    {
                        return reader.ReadToEnd();
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private void tpForm_Load(object sender, EventArgs e)
        {
            var logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
            XmlConfigurator.ConfigureAndWatch(logCfg);
            var logger = LogManager.GetLogger(typeof(Program));
            try
            {
                using (var db = new dbproductmainEntities())
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(" With Data");
                    sb.AppendLine("           As");
                    sb.AppendLine("       (");
                    sb.AppendLine(" select ta.node_id,ta.node_pid,ta.order_no,ta.node_path,tb.node_name,tb.map_key   from node_net ta inner join site_node tb on ta.node_id=tb.node_id where site_code='13385E2E95FA7FD3' and ta.node_pid='0'");
                    sb.AppendLine("        Union All");
                    sb.AppendLine("        select ta.node_id,ta.node_pid,ta.order_no,ta.node_path,tb.node_name,tb.map_key   from node_net ta inner join site_node tb on ta.node_id=tb.node_id  inner join  Data t on  ta.node_pid=t.node_path  ");
                    sb.AppendLine("        )");
                    sb.AppendLine("    select a.*,b.obj_table from  Data  a left join object_info b on a.map_key=b.obj_name");
                    var nodeinfos = db.ExecuteStoreQuery<NoteInfo>(sb.ToString());
                    RequestNode req = new RequestNode();
                    req.isSec = 1;
                    var nodes = nodeinfos.ToArray();
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        var nodeid = nodes[i].node_id;
                        req.Model.Add(new NodeStruct { AutoNo = Convert.ToInt32(nodes[i].node_id), NodeId = Convert.ToInt32(nodeid), NodeName = nodes[i].node_name, ChildNodeIds = nodes.Where(p => p.node_pid == nodes[i].node_path).Select(p => p.node_id).ToArray() });
                        if (!string.IsNullOrWhiteSpace(nodes[i].obj_table))
                        {
                            var sqlsb = new System.Text.StringBuilder();

                            sqlsb.AppendLine("select  " + Convert.ToInt32(nodes[i].node_id) + " as nodeId,* ");
                            sqlsb.AppendLine(" from  {0}  a left join  node_objdata b on a.IDLeaf=b.ID_Leaf  ");
                            sqlsb.AppendLine("  where a.[state]=@state and isnull(b.is_hot,0)=0 and b.node_id={1}");
                            sqlsb.AppendLine("  order by inputtime ");
                            DataTable needPublish = SQLHelper.GetTable(string.Format(sqlsb.ToString(), nodes[i].obj_table, nodeid),
                                                      new System.Data.SqlClient.SqlParameter[]{
                                 new System.Data.SqlClient.SqlParameter("@state",99)
                            });

                            sqlsb.Clear();

                            sqlsb.AppendLine("select isnull(max(num),0) score from (");
                            sqlsb.AppendLine(" select row_number()over(order by inputtime) as num  from  {0} a left join   ");
                            sqlsb.AppendLine(" node_objdata b on a.IDLeaf=b.ID_Leaf  ");
                            sqlsb.AppendLine(" where a.[state]=@state and b.is_hot=1 and  b.node_id={1}) b");
                            DataTable PublishedMaxScore = SQLHelper.GetTable(string.Format(sqlsb.ToString(), nodes[i].obj_table, nodeid),
                                                   new System.Data.SqlClient.SqlParameter[]{
                                 new System.Data.SqlClient.SqlParameter("@state",99)
                                
                            });

                            int score = Convert.ToInt32(PublishedMaxScore.Rows[0]["score"]);

                            DataTable fields = SQLHelper.GetTable("select * from dbo.object_dict where obj_table=@table ", new System.Data.SqlClient.SqlParameter[]{
                            new System.Data.SqlClient.SqlParameter("@table",nodes[i].obj_table)
                            });

                            int pushCount = 0;
                            var pushIDLeafs = new List<string>();
                            RequestScript rs = new RequestScript();
                            rs.isSec = Convert.ToInt32(Convert.ToInt32(nodes[i].node_id));
                            foreach (DataRow row in needPublish.Rows)
                            {
                                pushIDLeafs.Add(Convert.ToString(row["IDLeaf"]));
                                score = score + 1;
                                pushCount = pushCount + 1;
                                var resultstr = new List<string>();
                                foreach (DataRow r in fields.Rows)
                                {
                                    var key = Convert.ToString(r["obj_feild"]);
                                    var limit = 30000;
                                    string value = Convert.ToString(row[key]);
                                    string valueString = string.Empty;
                                    var num = (value.Length / limit);
                                    if (value.Length > 32766)
                                    {
                                        for (var k = 0; k <= num; k++)
                                        {
                                            if (k == num)
                                            {
                                                valueString += Uri.EscapeUriString(value.ToString().Substring(limit * k, value.Length - limit * k));
                                            }
                                            else
                                            {
                                                valueString += Uri.EscapeUriString(value.ToString().Substring(limit * k, limit));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        valueString = Uri.EscapeUriString(value);
                                    }

                                    resultstr.Add(String.Format("\"{0}\":\"{1}\"", Uri.EscapeUriString(key), valueString));
                                }
                                resultstr.Add(String.Format("\"{0}\":\"{1}\"", "nodeId", row["nodeId"].ToString()));
                                string str = string.Format("{0}{1}{2}", "{", string.Join(",", resultstr), "}");

                                rs.Model.Add(new Manuscript { AutoNo = score, content = str });

                                if (pushCount % 100 == 0 && pushCount >= 100)
                                {
                                    PostValue(ConfigurationManager.AppSettings["ScriptUrl"].ToString(), JsonConvert.SerializeObject(rs));
                                    rs = new RequestScript();
                                    string sql=string.Format("update  node_objdata set is_hot=1 where node_id=" + nodeid + " and ID_Leaf in ({0}{1}{2})","'", string.Join("','", pushIDLeafs),"'");
                                        db.ExecuteStoreCommand(sql);
                                    pushIDLeafs = new List<string>();
                                }
                                else
                                {
                                    if (pushCount == needPublish.Rows.Count)
                                    {
                                        PostValue(ConfigurationManager.AppSettings["ScriptUrl"].ToString(), JsonConvert.SerializeObject(rs));
                                        rs = new RequestScript();
                                        string sql=string.Format("update  node_objdata set is_hot=1 where node_id=" + nodeid + " and ID_Leaf in ({0}{1}{2})","'", string.Join("','", pushIDLeafs),"'");
                                        db.ExecuteStoreCommand(sql);
                                        pushIDLeafs = new List<string>();
                                    }
                                }
                            }
                        }
                    }
                    PostValue(ConfigurationManager.AppSettings["NodeUrl"].ToString(), JsonConvert.SerializeObject(req));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Error("程序退出");
                Application.Exit();
            }
            logger.Error("程序退出");
            Application.Exit();
        }
    }
}
