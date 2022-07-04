using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixOperation;
using AuxStructureLib;
using AuxStructureLib.ConflictLib;
using System.Data;
using AuxStructureLib.IO;

namespace AlgEMLib
{
    public abstract class AlgEM
    {
        public Matrix K = null;                                            //刚度矩阵
        public Matrix F = null;                                            //受力向量                            
        public Matrix D = null;                                            //移位结果

        public double[,] Test_K = null;                                            //刚度矩阵
        public double[,] Test_F = null;                                            //受力向量                            
        public double[,] Test_D = null;                                            //移位结果

        public double DisThreshold = -1;

        public double DisThresholdLP = -1;//线-面

        public double DisThresholdPP = -1;//面-面

        public string strPath = "";//文件路径
        public PolylineObject Polyline = null;//线对象

        public ProxiGraph ProxiGraph = null; //邻近图
        public List<ProxiGraph> PgList = null;//邻近图群组

        public ProxiGraph Truss = null; //Truss

        public SMap Map = null;            //地图
        public List<SMap> MapLists = null; //地图集合

        public List<GroupofMapObject> Groups = null;

        public ProxiGraph OriginalGraph = null;

        public List<ConflictBase> ConflictList = null; //冲突列表

        public double Scale = 10000;

        public bool isContinue = true;//是否继续迭代

        public bool isDragF = false;//是否有吸引力

        public bool IsTopCos = false;//是否强制约束拓扑关系

        public VoronoiDiagram VD = null;//通过骨架线生成的Voronoi图

        public int AlgType = 0;//0-Beams, 1-Sequence, 2-Combined

        public double PAT = 0.6;
        /// <summary>
        /// 不迭代
        /// </summary>
        public abstract void DoDispace();
     

        /// <summary>
        /// 迭代
        /// </summary>
        public abstract void DoDispaceIterate();

        /// <summary>
        /// 输出移位值和力
        /// </summary>
        /// <param name="ForceList">力列表</param>
        protected void OutputTotalDisplacementforProxmityGraph(ProxiGraph orginal, ProxiGraph current, SMap map)
        {
            if (orginal == null || current == null)
                return;
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "TotalDisplacement";
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("Dx", typeof(double));
            tableforce.Columns.Add("Dy", typeof(double));
            tableforce.Columns.Add("D", typeof(double));

            foreach (PolygonObject obj in map.PolygonList)
            {
                int id = obj.ID;
                ProxiNode oNode = orginal.GetNodebyTagIDandType(id, FeatureType.PolygonType);
                ProxiNode cNode = current.GetNodebyTagIDandType(id, FeatureType.PolygonType);
                if (oNode != null && cNode != null)
                {
                    double dx = cNode.X - oNode.X;
                    double dy = cNode.Y - oNode.Y;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    DataRow dr = tableforce.NewRow();
                    dr[0] = id;
                    dr[1] = dx;
                    dr[2] = dy;
                    dr[3] = d;
                    tableforce.Rows.Add(dr);
                }

            }
            TXTHelper.ExportToTxt(tableforce, this.strPath + @"-Displacement.txt");
        }
    
    }
}
