using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace AuxStructureLib.IO
{
    /// <summary>
    /// 测试文件输入、输出
    /// </summary>
    public class TestIO
    {
        /// <summary>
        /// 将一个表格，写一个XML文件
        /// </summary>
        /// <param name="strPath">文件路径</param>
        public  static void CreateTable(string strPath)
        {
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "Forces";
            tableforce.Columns.Add("ID", typeof(string));
            tableforce.Columns.Add("Fx", typeof(string));
            tableforce.Columns.Add("Fy", typeof(string));
            DataRow dr = tableforce.NewRow();
            dr[0] = "5";
            dr[1] = "1000";
            dr[2] = "1000";
            tableforce.Rows.Add(dr);
            ds.Tables.Add(tableforce);
            tableforce.AcceptChanges();
            ds.AcceptChanges();
            ds.WriteXml(strPath);
        }
        /// <summary>
        /// 符号XML文件
        /// </summary>
        /// <param name="strPath"></param>
        public static void CreateSylTable(string strPath)
        {
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "Symbol";
            tableforce.Columns.Add("ID", typeof(string));
            tableforce.Columns.Add("symbolID", typeof(string));
            tableforce.Columns.Add("LayerName", typeof(string));
            tableforce.Columns.Add("Size", typeof(string));
            tableforce.Columns.Add("BorderColor", typeof(string));
            tableforce.Columns.Add("FillColor", typeof(string));
            DataRow dr = tableforce.NewRow();
            dr[0] = "1";
            dr[1] = "20140304";
            dr[2] = "Road";
            dr[3] = "1.2";
            dr[4] = "Black";
            dr[5] = "Red";
            tableforce.Rows.Add(dr);
            ds.Tables.Add(tableforce);
            tableforce.AcceptChanges();
            ds.AcceptChanges();
            ds.WriteXml(strPath);
        }

        /// <summary>
        /// 读取XML文件返回DataTable对象
        /// </summary>
        /// <param name="strPath"></param>
        /// <returns></returns>
        public static DataTable ReadData(string strPath)
        {
            try
            {
                DataSet ds = new DataSet();
                //读取XML到DataSet
                ds.ReadXml(strPath);
                if (ds.Tables.Count > 0)
                {
                    return ds.Tables[0];
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
