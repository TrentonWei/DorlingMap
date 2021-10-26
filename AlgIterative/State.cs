using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using ESRI.ArcGIS.Geometry;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace AlgIterative
{

    /// <summary>
    /// 状态表
    /// </summary>
    /// 
    [Serializable]
    public class State : ICloneable
    {
        public int[] TrialPositionIDs = null;//位置表
        public List<Conflict> Conflicts = null;//个对象的冲突表
        private double stateCost = -1;//State_Cost对应的字段
        /// <summary>
        /// 构造函数
        /// </summary>
        public State()
        {

        }
        /// <summary>
        /// 两个状态是否相同
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool IsEqual(State state)
        {
            int n = this.TrialPositionIDs.Length;
            int m = state.TrialPositionIDs.Length;
            if (m != n)
                return false;
            for (int i = 0; i < n; i++)
            {
                if (this.TrialPositionIDs[i] != state.TrialPositionIDs[i])
                    return false;
            }
            return true;
        }
        /// <summary>
        /// 状态值
        /// </summary>
        public double State_Cost
        {
            get
            {
                return stateCost;

            }
            set
            {
                stateCost = value;
            }
        }
        /// <summary>
        /// 获取与某个对象相关的冲突
        /// </summary>
        /// <param name="Obj">对象</param>
        /// <returns>冲突列表</returns>
        public List<Conflict>  GetObjectConflict(SDS_PolygonO Obj)
        {
            List<Conflict> OConflicts = new List<Conflict>();
            foreach (Conflict curConflict in this.Conflicts)
            {
                if(curConflict.Obj1==Obj||curConflict.Obj2==Obj)
                {
                    OConflicts.Add(curConflict);
                }
            }
            return OConflicts;
        }

        public void UpdateState(State bestState, SDS MapSDS,DispVectorTemplate DispTemplate)
        {
             SDS_PolygonO move_Obj=null;
             int move_Pos = 0 ;
             int move_Pos0 = 0;
             double dx;
             double dy; 
             double dx0; 
             double dy0;

            int n=this.TrialPositionIDs.Length;
            for (int i = 0; i < n; i++)
            {
                move_Obj = MapSDS.PolygonOObjs[i];
                move_Pos = bestState.TrialPositionIDs[i];
                move_Pos0 = this.TrialPositionIDs[i];
                if (move_Pos != move_Pos0)
                {
                    dx0 = DispTemplate.TrialPosiList[this.TrialPositionIDs[i]].Dx;
                    dy0 = DispTemplate.TrialPosiList[this.TrialPositionIDs[i]].Dy;
                    dx = DispTemplate.TrialPosiList[bestState.TrialPositionIDs[i]].Dx;
                    dy = DispTemplate.TrialPosiList[bestState.TrialPositionIDs[i]].Dy;
                    move_Obj.Translate((-1.0) * dx0, (-1.0) * dy0);
                    move_Obj.Translate(dx, dy);
                }
            }
        }
        /// <summary>
        /// 更新状态（通用模版）
        /// </summary>
        /// <param name="indexofO"></param>
        /// <param name="move_Obj"></param>
        /// <param name="move_Pos"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dx0"></param>
        /// <param name="dy0"></param>
        /// <param name="oNewConflicts"></param>
        /// <param name="oCurConflicts"></param>
        /// <param name="newCost"></param>
        public void UpdateState(int indexofO, SDS_PolygonO move_Obj, int move_Pos,
            double dx, double dy, double dx0, double dy0,
            List<Conflict> oNewConflicts, List<Conflict> oCurConflicts,
            double newCost)
        {
            if (dx0 != 0 || dy0 != 0)
            {
                 move_Obj.Translate((-1.0) * dx0, (-1.0) * dy0);//更新坐标
            }
            if (dx != 0 || dy != 0)
            {
                move_Obj.Translate(dx, dy);//更新坐标
            }
       
            this.TrialPositionIDs[indexofO] = move_Pos;//更新状态码
            if (oCurConflicts != null)
            {
                foreach (Conflict c in oCurConflicts)
                {
                    this.Conflicts.Remove(c);
                }
            }
            if (oNewConflicts != null)
            {
                this.Conflicts.AddRange(oNewConflicts);
            }
            this.State_Cost = newCost;
        }

        /// <summary>
        /// 更新状态（V图优化后模版）
        /// </summary>
        /// <param name="indexofO"></param>
        /// <param name="move_Obj"></param>
        /// <param name="move_Pos"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dx0"></param>
        /// <param name="dy0"></param>
        /// <param name="oNewConflicts"></param>
        /// <param name="oCurConflicts"></param>
        /// <param name="newCost"></param>
        public void UpdateState(int indexofO, SDS_PolygonO move_Obj, int move_Pos,
            double dx, double dy, double dx0, double dy0,
            List<Conflict> oNewConflicts, List<Conflict> oCurConflicts,
            double newCost, SMap map, SMap map1)
        {
            PolygonObject o = map.PolygonList[indexofO];
            PolygonObject o1 = map1.PolygonList[indexofO];
            if (dx0 != 0 || dy0 != 0)
            {
                move_Obj.Translate((-1.0) * dx0, (-1.0) * dy0);//更新坐标
                o.Translate((-1.0) * dx0, (-1.0) * dy0);//更新坐标
                o1.Translate((-1.0) * dx0, (-1.0) * dy0);//更新坐标
            }
            if (dx != 0 || dy != 0)
            {
                move_Obj.Translate(dx, dy);//更新坐标
                o.Translate(dx, dy);//更新坐标
                o1.Translate(dx, dy);//更新坐标
            }

            this.TrialPositionIDs[indexofO] = move_Pos;//更新状态码
            if (oCurConflicts != null)
            {
                foreach (Conflict c in oCurConflicts)
                {
                    this.Conflicts.Remove(c);
                }
            }
            if (oNewConflicts != null)
            {
                this.Conflicts.AddRange(oNewConflicts);
            }
            this.State_Cost = newCost;
        }
        /// <summary>
        /// 输出冲突邻近图
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="map"></param>
        /// <param name="pg"></param>
        /// <param name="prj"></param>
        public void WriteConflict2File(string filePath, string fileName, SMap map, ProxiGraph pg,esriSRProjCS4Type prj)
        {
            ProxiGraph pg1 = new ProxiGraph();
            pg1.CreateProxiGraphfrmConflicts(map, this.Conflicts);
            foreach (ProxiEdge edge in pg1.EdgeList)
            {
                foreach (ProxiEdge edge1 in pg.EdgeList)
                {
                    if ((edge.Node1.TagID == edge1.Node1.TagID && edge.Node1.FeatureType == edge1.Node1.FeatureType && edge.Node2.TagID == edge1.Node2.TagID && edge.Node2.FeatureType == edge1.Node2.FeatureType)
                        || (edge.Node1.TagID == edge1.Node2.TagID && edge.Node1.FeatureType == edge1.Node2.FeatureType && edge.Node2.TagID == edge1.Node1.TagID && edge.Node2.FeatureType == edge1.Node1.FeatureType))
                    {
                        edge.NearestEdge = edge1.NearestEdge;
                    }
                }
            }
            pg1.WriteProxiGraph2Shp(filePath, fileName, prj);
        }



        #region ICloneable 成员

        public object Clone()
        {
            BinaryFormatter bf = new BinaryFormatter();

            MemoryStream ms = new MemoryStream();

            bf.Serialize(ms, this);

            ms.Seek(0, SeekOrigin.Begin);

            return bf.Deserialize(ms);
        }

        #endregion

        /// <summary>
        /// 复制状态
        /// </summary>
        /// <returns></returns>
        public State Copy()
        {
            int n=this.TrialPositionIDs.Length;
            State state = new State();
            state.TrialPositionIDs = new int[n];
            for (int i = 0; i < n; i++)
            {
                state.TrialPositionIDs[i] = this.TrialPositionIDs[i];
            }
            return state;
        }
        /// <summary>
        /// 复制并改变状态（仅改变位置数组）
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public State ChangeNewState(Move move)
        {
            State state = Copy();
            state.TrialPositionIDs[move.move_Obj] = move.move_Pos;
            return state;
        }
    }
}
