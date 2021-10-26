using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using AuxStructureLib.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// 地图对象分组
    /// </summary>
    public class GroupofMapObject
    {
        //对象列表
        private List<MapObject> listofObject = null;
        public List<MapObject> ListofObjects
        {
            get { return listofObject; }
            set { this.listofObject = value; }
        }

        private int id=-1;
        public int ID
        {
            get { return id; }
            set { this.id = value; }
        }

        /// <summary>
        /// 分组面积
        /// </summary>
        private double area = -1;
        /// <summary>
        /// 分组的总面积
        /// </summary>
        public double Area
        {
            get
            {
                if (area == -1)
                {
                    foreach (MapObject curObj in this.ListofObjects)
                    {
                        if (curObj.FeatureType == FeatureType.PolygonType)
                        {
                            area += (curObj as PolygonObject).Area;
                        }
                    }
                    return area;
                }
                //计算
                else
                {
                    return area;
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public GroupofMapObject()
        {
            this.listofObject = new List<MapObject>();
            this.id = -1;
        }
        /// <summary>
        /// 是否包含对象
        /// </summary>
        /// <param name="TagID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsContainsObject(MapObject obj)
        {
            if(this.listofObject==null||this.listofObject.Count==0)
                return false;
            foreach (MapObject curObj in this.ListofObjects)
            {
                if (curObj.FeatureType == obj.FeatureType && curObj.ID == obj.ID)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 获取分组
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="groups">分组</param>
        /// <returns>分组</returns>
        public static GroupofMapObject GetGroup(MapObject obj, List<GroupofMapObject> groups)
        {
            if (groups == null || groups.Count == 0)
                return null;
            foreach (GroupofMapObject group in groups)
            {
                if (group.IsContainsObject(obj))
                    return group;
            }
            return null;
        }

        /// <summary>
        /// 获取分组
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="groups">分组</param>
        /// <returns>分组</returns>
        public static GroupofMapObject GetGroup(int TagID,List<GroupofMapObject> groups)
        {
            if (groups == null || groups.Count == 0)
                return null;
            foreach (GroupofMapObject group in groups)
            {
                if (TagID==group.ID)
                    return group;
            }
            return null;
        }

        /// <summary>
        /// 读取分组
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public static List<GroupofMapObject> ReadGroups(string FileName,SMap map)
        {
            //读文件========
            DataTable dt = TestIO.ReadData(FileName);
            if (dt == null||dt.Rows.Count==0)
                return null;
            List<GroupofMapObject> groupList = new List<GroupofMapObject>();
            foreach (DataRow curR in dt.Rows)
            {
                int ID = Convert.ToInt32(curR[0]);
                string Buildings = Convert.ToString(curR[1]);
                string[] sArray = Buildings.Split(new char[] { ',' });
                int n = sArray.Length;
                if (n <= 0)
                    continue;
                int[] bIDs = new int[n];
                GroupofMapObject group = new GroupofMapObject();
                for (int i = 0; i < n; i++)
                {
                    group.ID = ID;
                    bIDs[i] = Convert.ToInt32(sArray[i]);
                    group.ListofObjects.Add(map.GetObjectbyID(bIDs[i], FeatureType.PolygonType));
                }
                groupList.Add(group);
            }
            return groupList;
        }

        ///--其他属性
    }
}
