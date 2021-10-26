using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoadDisAlg
{
    /// <summary>
    /// 生成子网
    /// </summary>
    public class GenerateSunNetWorks
    {
        AlgSnakes _algSnakes = null;

        public List<RoadNetWork> RoadNetWorkList = null;

        public GenerateSunNetWorks(AlgSnakes algSnakes)
        {
            _algSnakes = algSnakes;
        }

        #region 子网处理
        /// <summary>
        /// 获取初始子网2013-8-30
        /// </summary>
        /// <param name="k">力的传播幅度系数</param>
        /// <returns></returns>
        private void GetInitSubNetWorks(int k)
        {
            RoadNetWorkList = new List<RoadNetWork>();
            _algSnakes.ComForce_Triangle();

            for (int i = 0; i < _algSnakes.forceList.Length; i++)
            {
                Force curForce = _algSnakes.forceList[i];
                if (curForce.F > 0.0000001)
                {
                    float range = (float)(curForce.F * k * AlgSnakes.scale / 1000);//传播范围
                    ConnNode node = ConnNode.GetConnNodebyPID(_algSnakes._NetWork.ConnNodeList, i);

                    if (node != null)
                    {
                        RoadNetWork curRoadNetWork = ConstrSubNetWorkfrmNode(node, range);
                        continue;
                    }
                    else
                    {

                        foreach (Road curR in _algSnakes._NetWork.RoadList)
                        {

                            for (int j = 0; j < curR.PointList.Count; j++)
                            {
                                if (curR.PointList[j] == i)
                                {
                                    RoadNetWork curRoadNetwork = ConstrSubNetWorkfrmRoad(i, curR, range);
                                    RoadNetWorkList.Add(curRoadNetwork);
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 构造子网(路段中间节点)
        /// </summary>
        /// <returns>返回子网</returns>
        private RoadNetWork ConstrSubNetWorkfrmRoad(int pID, Road road, float range)
        {
            RoadNetWork curNetWork = new RoadNetWork(_algSnakes._NetWork.RoadLyrInfoList);
            int index = 0;
            float rfRange = range;
            float rbRange = range;
            int fNIndex = -1;
            int bNIndex = -1;
            for (int i = 0; i < road.PointList.Count; i++)
            {
                if (_algSnakes._NetWork.PointList[road.PointList[i]].ID == pID)
                {
                    index = i;
                    break;
                }
            }
            if (index == 0)
                return null;

            int fc = index;//前半部分点数
            int bc = road.PointList.Count - index;//后半部分点数
            //前半部
            for (int i = fc - 1; i >= 0; i--)
            {
                PointCoord p1 = _algSnakes._NetWork.PointList[road.PointList[i + 1]];
                PointCoord p2 = _algSnakes._NetWork.PointList[road.PointList[i]];
                float l = (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                rfRange = rfRange - l;//剩下的传播范围
                if (rfRange <= 0)
                {
                    //传播终止
                    fNIndex = i;
                    break;
                }
            }

            //后半部分
            for (int i = index; i < bc; i++)
            {
                PointCoord p1 =_algSnakes._NetWork.PointList[road.PointList[i]];
                PointCoord p2 = _algSnakes._NetWork.PointList[road.PointList[i + 1]];
                float l = (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                rbRange = rbRange - l;//剩下的传播范围
                if (rbRange <= 0)
                {
                    //传播终止
                    bNIndex = i;
                    break;
                }
            }
            if (fNIndex == -1 && bNIndex == -1)
            {
                curNetWork.RoadList.Add(road);

                //两端传播
                ConnNode snode = ConnNode.GetConnNodebyPID(_algSnakes._NetWork.ConnNodeList, 0);
                ConnNode enode = ConnNode.GetConnNodebyPID(_algSnakes._NetWork.ConnNodeList, road.PointList.Count - 1);

                this.GetRoadsbyNode(snode, rfRange, road.RID, ref curNetWork);
                this.GetRoadsbyNode(enode, rbRange, road.RID, ref curNetWork);
            }
            else if (fNIndex == -1 && bNIndex != -1)//超出该路段情况
            {
                Road newRoad = new Road(null, null);
                newRoad.RID = road.RID;
                for (int i = 0; i <= bNIndex; i++)
                {
                    newRoad.PointList.Add(road.PointList[i]);
                }
                curNetWork.RoadList.Add(newRoad);
                //向前传播
                ConnNode snode = ConnNode.GetConnNodebyPID(_algSnakes._NetWork.ConnNodeList, 0);
                this.GetRoadsbyNode(snode, rfRange, road.RID, ref curNetWork);
            }
            else if (fNIndex != -1 && bNIndex == -1)
            {
                Road newRoad = new Road(null, null);
                newRoad.RID = road.RID;
                for (int i = fNIndex; i < road.PointList.Count; i++)
                {
                    newRoad.PointList.Add(road.PointList[i]);
                }
                curNetWork.RoadList.Add(newRoad);
                //向后传播
                ConnNode enode = ConnNode.GetConnNodebyPID(_algSnakes._NetWork.ConnNodeList, road.PointList.Count - 1);
                this.GetRoadsbyNode(enode, rbRange, road.RID, ref curNetWork);
            }
            else if (fNIndex != -1 && bNIndex != -1)
            {
                Road newRoad = new Road(null, null);
                newRoad.RID = road.RID;
                for (int i = fNIndex; i <= bNIndex; i++)
                {
                    newRoad.PointList.Add(road.PointList[i]);
                }
                curNetWork.RoadList.Add(newRoad);
                //不传播
            }
            return curNetWork;
        }
        /// <summary>
        /// 通过端点获得子网
        /// </summary>
        /// <param name="node">端点</param>
        /// <param name="range">范围</param>
        /// <returns>返回结果</returns>
        private RoadNetWork ConstrSubNetWorkfrmNode(ConnNode node, float range)
        {
            RoadNetWork netWork = new RoadNetWork(_algSnakes._NetWork.RoadLyrInfoList);
            List<int> rIDList = node.ConRoadList;

            if (rIDList.Count == 1)
            {
                //该端点出没有与其他路段关联,且是受力点，需要进一步研究8-30
            }
            foreach (int curRID in rIDList)
            {
                Road curRoad = _algSnakes._NetWork.RoadList[curRID];
                GetSubRoadbyRoad(node, curRoad, range, ref netWork);
            }
            return netWork;
        }

        /// <summary>
        /// 在端点处的传播到达的道路
        /// </summary>
        /// <param name="node">端点</param>
        /// <param name="range">传播范围</param>
        /// <param name="netWork">结果道路网</param>
        /// <param name="rID">道路号</param>
        private void GetRoadsbyNode(ConnNode node, float range, int rID, ref RoadNetWork netWork)
        {
            List<int> rIDList = node.ConRoadList;
            if (rIDList.Count == 1)
            {
                //该端点出没有与其他路段关联，需要做延长线
                return;
            }
            foreach (int curRID in rIDList)
            {
                if (curRID == rID)
                    continue;
                Road curRoad =_algSnakes._NetWork.RoadList[curRID];
                GetSubRoadbyRoad(node, curRoad, range, ref netWork);
            }
        }

        /// <summary>
        /// 从一个端点出发获取部分道路段
        /// </summary>
        /// <param name="road">路段</param>
        /// <param name="range">传播范围</param>
        private void GetSubRoadbyRoad(ConnNode node, Road road, float range, ref RoadNetWork netWork)
        {
            int index = -1;
            int nodePID = node.PointID;
            if (nodePID == road.FNode)
            {
                for (int i = 1; i < road.PointList.Count; i++)
                {
                    PointCoord p1 = _algSnakes._NetWork.PointList[road.PointList[i - 1]];
                    PointCoord p2 = _algSnakes._NetWork.PointList[road.PointList[i]];
                    float l = (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                    range = range - l;//剩下的传播范围
                    if (range <= 0)
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                {
                    ConnNode nextNode = ConnNode.GetConnNodebyPID(_algSnakes._NetWork.ConnNodeList, road.PointList[road.PointList.Count - 1]);
                    GetRoadsbyNode(nextNode, range, road.RID, ref netWork);
                }
                else
                {
                    Road newRoad = new Road(null, null);
                    newRoad.RID = road.RID;
                    for (int i = 0; i <= index; i++)
                    {
                        newRoad.PointList.Add(road.PointList[i]);
                    }
                    netWork.RoadList.Add(newRoad);
                }
            }
            else
            {
                for (int i = road.PointList.Count - 2; i >= 0; i--)
                {
                    PointCoord p1 = _algSnakes._NetWork.PointList[road.PointList[i + 1]];
                    PointCoord p2 = _algSnakes._NetWork.PointList[road.PointList[i]];
                    float l = (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                    range = range - l;//剩下的传播范围
                    if (range <= 0)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    ConnNode nextNode = ConnNode.GetConnNodebyPID(_algSnakes._NetWork.ConnNodeList, road.PointList[0]);
                    GetRoadsbyNode(nextNode, range, road.RID, ref netWork);
                }
                else
                {
                    Road newRoad = new Road(null, null);
                    newRoad.RID = road.RID;
                    for (int i = index; i <= road.PointList.Count; i++)
                    {
                        newRoad.PointList.Add(road.PointList[i]);
                    }
                    netWork.RoadList.Add(newRoad);
                }
            }
        }

        #endregion
    }
}
