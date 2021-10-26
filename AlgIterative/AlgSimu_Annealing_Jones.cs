using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
//主要实现以Jones为代表的模拟退火、遗传算法、禁忌搜索等算法
namespace AlgIterative
{
   
    /// <summary>
    /// Jones模拟退火
    /// </summary>
    public class AlgSimu_Annealing_Jones
    {
         //简单数据结构数据
        public SDS MapSDS = null;
        //各类冲突权值
        public double PLCost = 10;
        public double PPCost = 1;
        public double DispCost = 1;
        public double DTol = 7.5;

        //算法参数
        public double V = 3.0;//初始温度
        public double X = 90.0;//降温百分比
        public int W = 40;//Maximum repositioning times before a temperature decreasing
        public int Y = 20;//Maximum successful repositioning times before a temperature before a temperature decreasing
        public double Tn = 50;
        public double Z = 50;//Maximum number of temperature stages
        //
        public State State = null;//状态
        public DispVectorTemplate DispTemplate = null;//模版
        ConflictDetection ConflictDetector = null;

        public AlgSimu_Annealing_Jones(SDS map)
        {
            State = new State();
            InitDispTemplate();
            MapSDS = map;
            ConflictDetector = new ConflictDetection(MapSDS);    
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map"></param>
        /// <param name="v">初始温度</param>
        /// <param name="x">降温比例</param>
        /// <param name="w">最大试探数</param>
        /// <param name="y">最大成功试探数</param>
        /// <param name="z">降温次数</param>
        public AlgSimu_Annealing_Jones(SDS map,double v,double x,int w,int y,double tn)
        {
            this.V = v;
            this.X = x;
            this.W = w;
            this.Y = y;
            this.Tn = tn;
            State = new State();
            InitDispTemplate();
            MapSDS = map;
            ConflictDetector = new ConflictDetection(MapSDS);

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="plcost">线面冲突权值</param>
        /// <param name="ppcost">面面冲突权值</param>
        /// <param name="dcost">移位权重</param>
        /// <param name="dtol">距离阈值</param>
        public AlgSimu_Annealing_Jones(SDS map, double plcost, double ppcost, double dcost, double dtol)
        {
            PLCost = plcost;
            PPCost = ppcost;
            DispCost = dcost;
            DTol = dtol;
            State = new State();
            InitDispTemplate();
            MapSDS = map;
            ConflictDetector = new ConflictDetection(MapSDS);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="plcost">线面冲突权值</param>
        /// <param name="ppcost">面面冲突权值</param>
        /// <param name="dcost">移位权重</param>
        /// <param name="dtol">距离阈值</param>
        public AlgSimu_Annealing_Jones(SDS map, 
            double plcost, double ppcost, double dcost,//P-LCOST、PPCOST、DCOST
            double dtol,//距离阈值
            double v,double x,int w,int y,double Tn)//初始温度、降温速率、测试次数、成功次数、降温次数
        {
            PLCost = plcost;
            PPCost = ppcost;
            DispCost = dcost;
            DTol = dtol;
            this.V = v;
            this.X = x;
            this.W = w;
            this.Y = y;
           // this.Z = z;
            State = new State();
            InitDispTemplate();
            MapSDS = map;
            ConflictDetector = new ConflictDetection(MapSDS);
        }

        /// <summary>
        /// 初始化移位向量模版-16方向
        /// </summary>
        private void InitDispTemplate()
        {
            List<TrialPosiDisGroup> TPDGList = new List<TrialPosiDisGroup>();
            TrialPosiDisGroup g1 = new TrialPosiDisGroup(this.DTol/3.0, 0, Math.PI / 2);
            TrialPosiDisGroup g2 = new TrialPosiDisGroup((2*this.DTol) / 3.0, Math.PI / 8, Math.PI / 4);
            TrialPosiDisGroup g3 = new TrialPosiDisGroup(DTol, 0, Math.PI / 8);
           // TrialPosiDisGroup g4= new TrialPosiDisGroup(5 , 0, Math.PI / 16);
            TPDGList.Add(g1);
            TPDGList.Add(g2);
            TPDGList.Add(g3);
            //TPDGList.Add(g4);
            DispTemplate = new DispVectorTemplate(TPDGList);
            DispTemplate.CalTriPosition();
        }

        /// <summary>
        /// 计算初始目标值
        /// 思路：找出所有冲突，然后移位
        /// </summary>
        /// <returns></returns>
        public double Evaluate_InitState()
        {
            List<Conflict> conflictList = ConflictDetector.ConflictDetect(DTol);
            State.Conflicts = conflictList;
            int n = MapSDS.PolygonOObjs.Count;
            State.TrialPositionIDs = new int[n];
            for (int i = 0; i < n;i++ )
            {
                State.TrialPositionIDs[i] = 0;
            }
            this.State.State_Cost = CostFunction(State);
            return this.State.State_Cost;
        }

        /// <summary>
        /// 通过更改一个位置计算新的目标值
        /// 思路：先将CurState中与Obj相关的所有冲突揪出来，然后重新计算Obj的冲突
        /// 两者对比评价值，他们的差别即是两个不同状态的差别
        /// </summary>
        /// <param name="CurState">当前状态</param>
        /// <param name="Obj">当前假设要移位的对象</param
        /// <param name="Trial_PositionNO">当前试探的新位置</param>
        /// <param name="ObjCurConflicts">对象当前涉及的冲突（输出）</param>
        /// <param name="ObjNewConflicts">假设对象移位后涉及的冲突（输出）</param>
        /// <param name="index">对象的索引号（在所有对象中的排列号）（输出）</param>
        /// <param name="dx">移位X（输出）</param>
        /// <param name="dy">移位Y（输出）</param>
        /// <param name="dx0">移位X（输出）</param>
        /// <param name="dy0">移位Y（输出）</param>
        /// <returns>新的评价值</returns>
        private double Evaluate_NewState(State CurState, SDS_PolygonO Obj, int Trial_PositionNO,
            out List<Conflict>  ObjCurConflicts,out List<Conflict> ObjNewConflicts,
            out int index ,out double dx,out double dy,out double dx0,out double dy0,out bool isInverted )
        {
            isInverted = false;

            dx0 = 0;
            dy0 = 0;
            index=0;
            ObjCurConflicts=null;
            ObjNewConflicts=null;
            //找到对象在数组中的位置
            for(index=0;index<this.MapSDS.PolygonOObjs.Count ;index++)
            {
                if(this.MapSDS.PolygonOObjs[index]==Obj)
                    break;
            }
            if (CurState.TrialPositionIDs[index] != 0)
            {
                dx0 = DispTemplate.TrialPosiList[CurState.TrialPositionIDs[index]].Dx;
                dy0 = DispTemplate.TrialPosiList[CurState.TrialPositionIDs[index]].Dy;
                Obj.Translate((-1.0) * dx0, (-1.0) * dy0);
            }

            dx = this.DispTemplate.TrialPosiList[Trial_PositionNO].Dx;
            dy = this.DispTemplate.TrialPosiList[Trial_PositionNO].Dy;
            Obj.Translate(dx, dy);

            //首先判断是否存在三角形的穿越
            if (this.HasTriangleInverted(Obj))
            {
                 Obj.Translate((-1.0)*dx,(-1.0)* dy);
                 Obj.Translate(dx0, dy0);
                 isInverted = true;
                 return CurState.State_Cost;
            }

            ObjNewConflicts = this.ConflictDetector.ObjectConflictDetect(Obj, this.DTol);
            double oNewCost = CostFunction(ObjNewConflicts, Trial_PositionNO);
            ObjCurConflicts=CurState.GetObjectConflict(Obj);
            double oCurCost = CostFunction(ObjCurConflicts, CurState.TrialPositionIDs[index]);
            double NewCost=CurState.State_Cost+(oNewCost-oCurCost);

            Obj.Translate((-1.0) * dx, (-1.0) * dy);//还原
            Obj.Translate(dx0, dy0);//还原
            return NewCost;
        }

        /// <summary>
        /// 判断是否存在三角形的穿越
        /// </summary>
        /// <param name="Polygon">多边i型你</param>
        /// <returns></returns>
        /// 
        private bool HasTriangleInverted(SDS_PolygonO Polygon)
        {
            List<SDS_Triangle> triList = Polygon.GetSurroundingTris();
            foreach (SDS_Triangle curTri in triList)
            {
                if (!curTri.IsAnticlockwise())
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 计算总状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(State state)
        {
            double cost=0;
            foreach (Conflict conflict in state.Conflicts)
            {
                if (conflict.ConflictType == @"PP")
                {
                    cost += (this.PPCost * (this.DTol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {
                    cost += (this.PLCost * (this.DTol - conflict.Distance));
                }
            }
            for(int i=0;i<state.TrialPositionIDs.Length;i++)
            {
                if (state.TrialPositionIDs[i] != 0)
                {
                    cost += this.DispCost * DispTemplate.TrialPosiList[state.TrialPositionIDs[i]].Distance;
                   // cost += 1;
                }
            }
            return cost;
        }

        /// <summary>
        /// 计算单个对象的状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(List<Conflict> Conflicts,int PosIndex)
        {
            double cost = 0;
            if (Conflicts != null && Conflicts.Count > 0)
            {
                foreach (Conflict conflict in Conflicts)
                {
                    if (conflict.ConflictType == @"PP")
                    {
                        cost += (this.PPCost * (this.DTol-conflict.Distance));
                    }
                    else if (conflict.ConflictType == @"PL")
                    {
                        cost += (this.PLCost * (this.DTol - conflict.Distance));
                        //cost += (this.PLCost * conflict.Distance);

                    }
                }
            }
           
            if (PosIndex != 0)
            {
                double d = this.DispTemplate.TrialPosiList[PosIndex].Distance;
                cost += this.DispCost * d;
                //cost += this.DispCost * DispTemplate.TrialPosiList[PosIndex].Distance;
            }
            return cost;
        }
        /// <summary>
        /// 执行算法
        /// </summary>
        public double DoAlgSimu_Annealing_Jones(out int InitCN,out int timesp)
        {
            DateTime Time1 = DateTime.Now;
            timesp = 0;
            double A = 0;
            double a = this.Evaluate_InitState();
            InitCN = this.State.Conflicts.Count;
            A = a;
            double b = 0;
            if (a == 0)//或小于摸个阈值
                return 0;
            else
            {
                State cur_State = null;
                cur_State = this.State;

                int n = this.MapSDS.PolygonOObjs.Count;
                int TriPosiCount = this.DispTemplate.TrialPosiList.Count;

                double T = this.V;
                int number_of_temps = 0;
                int positions_tested = 0;
                int successful_tested = 0;

                bool isContinue=true;


                SDS_PolygonO move_Obj = null;
                int move_Pos = -1;
                List<Conflict> oNewConflict = null;//对象新的冲突
                List<Conflict> oCurConflict = null;//对象当前冲突
                int index = -1;//对象在数组中的位置
                double Dx = -1;
                double Dy = -1;
                double Dx0 = 0;
                double Dy0 =0;

                int indexofObj = -1;
                int indexofPos = -1;
                SDS_PolygonO curObj;
                double dE = -1;
                while (a != 0 && isContinue)
                {
                    indexofObj = this.GenerateRandomNumber(n);
                    curObj = MapSDS.PolygonOObjs[indexofObj];
                    indexofPos = this.GenerateRandomNumber(TriPosiCount);


                    List<Conflict> oNewC = null;//对象新的冲突
                    List<Conflict> oCurC = null;//对象当前冲突
                    int id;
                    double dx;
                    double dy;
                    double dx0;
                    double dy0;
                    bool isInverted;
                    b = this.Evaluate_NewState(cur_State, curObj, indexofPos, out oCurC, out oNewC, out id, out dx, out dy, out dx0,out dy0,out isInverted);
                    if (isInverted == true)//三角形穿越
                        continue;
                    dE = b - a;
                    positions_tested++;

                    if (dE < 0)
                    {
                        move_Obj = curObj;
                        move_Pos = indexofPos;
                        a = b;
                        oNewConflict = oNewC;
                        oCurConflict = oCurC;
                        index = id;
                        Dx = dx;
                        Dy = dy;
                        Dx0 = dx0;
                        Dy0 = dy0;
                        //Reture2OriginalPos(move_Obj, cur_State);
                        cur_State.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                        successful_tested++;
                    }
                    /*else
                    {
                        double P =Math.Exp((-1.0) * dE / T);
                        double R = this.GenerateRandomFoat();
                        if (R < P)
                        {
                            move_Obj = curObj;
                            move_Pos = indexofPos;
                            a = b;
                            oNewConflict = oNewC;
                            oCurConflict = oCurC;
                            index = id;
                            Dx = dx;
                            Dy = dy;
                            Dx0 = dx0;
                            Dy0 = dy0;
                            //Reture2OriginalPos(move_Obj, cur_State);
                            cur_State.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                            successful_tested++;
                        }
                    }*/
                    if (positions_tested > this.W || successful_tested > this.Y)
                    {
                        if (T < this.Tn/*number_of_temps > this.Z || (successful_tested == 0)*/)
                        {
                            isContinue = false;
                        }
                        else
                        {
                            T = T * this.X / 100;
                            successful_tested = 0;
                            positions_tested = 0;
                            number_of_temps++;
                        }
                    }
                    
                }
            }


            DateTime Time2 = DateTime.Now;

            // Difference in days, hours, and minutes.
            TimeSpan ts = Time2 - Time1;
            // Difference in ticks.
             timesp =(int) ts.TotalMilliseconds; 
            return A;
        }

        /// <summary>
        /// 执行算法
        /// </summary>
        public double DoAlgSimu_Annealing_JonesI(out int InitCN, out int timesp)
        {
            DateTime Time1 = DateTime.Now;
            timesp = 0;
            double A = 0;
            double a = this.Evaluate_InitState();
            InitCN = this.State.Conflicts.Count;
            A = a;
            double b = 0;
            if (a == 0)//或小于摸个阈值
                return 0;
            else
            {
                State cur_State = null;
                State best_State = null;
                best_State = this.State.Clone() as State;
                cur_State = this.State;

                int n = this.MapSDS.PolygonOObjs.Count;
                int TriPosiCount = this.DispTemplate.TrialPosiList.Count;

                double T = this.V;
                int number_of_temps = 0;
                int positions_tested = 0;
                int successful_tested = 0;

                bool isContinue = true;


                SDS_PolygonO move_Obj = null;
                int move_Pos = -1;
                List<Conflict> oNewConflict = null;//对象新的冲突
                List<Conflict> oCurConflict = null;//对象当前冲突
                int index = -1;//对象在数组中的位置
                double Dx = -1;
                double Dy = -1;
                double Dx0 = 0;
                double Dy0 = 0;

                int indexofObj = -1;
                int indexofPos = -1;
                SDS_PolygonO curObj;
                double dE = -1;
                while (a != 0 && isContinue)
                {
                    indexofObj = this.GenerateRandomNumber(n);
                    curObj = MapSDS.PolygonOObjs[indexofObj];
                    indexofPos = this.GenerateRandomNumber(TriPosiCount);


                    List<Conflict> oNewC = null;//对象新的冲突
                    List<Conflict> oCurC = null;//对象当前冲突
                    int id;
                    double dx;
                    double dy;
                    double dx0;
                    double dy0;
                    bool isInverted;
                    b = this.Evaluate_NewState(cur_State, curObj, indexofPos, out oCurC, out oNewC, out id, out dx, out dy, out dx0, out dy0, out isInverted);
                    if (isInverted == true)//三角形穿越
                        continue;
                    dE = b - a;
                    positions_tested++;

                    if (dE < 0)
                    {
                        move_Obj = curObj;
                        move_Pos = indexofPos;
                        a = b;
                        oNewConflict = oNewC;
                        oCurConflict = oCurC;
                        index = id;
                        Dx = dx;
                        Dy = dy;
                        Dx0 = dx0;
                        Dy0 = dy0;
                        //Reture2OriginalPos(move_Obj, cur_State);
                        cur_State.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                        successful_tested++;

                        if (a < best_State.State_Cost)
                        {
                            best_State = cur_State.Clone() as State;
                        }
                       
                    }
                    else
                    {
                        double P =Math.Exp((-1.0) * dE / T);
                        double R = this.GenerateRandomFoat();
                        if (R < P)
                        {
                            move_Obj = curObj;
                            move_Pos = indexofPos;
                            a = b;
                            oNewConflict = oNewC;
                            oCurConflict = oCurC;
                            index = id;
                            Dx = dx;
                            Dy = dy;
                            Dx0 = dx0;
                            Dy0 = dy0;
                            //Reture2OriginalPos(move_Obj, cur_State);
                            cur_State.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                            successful_tested++;
                        }
                    }
                    if (positions_tested > this.W || successful_tested > this.Y)
                    {
                        if (T<0.001/*number_of_temps > this.Z *//*|| (successful_tested == 0)*/)
                        {
                            isContinue = false;
                            this.State = best_State;
                        }
                        else
                        {
                            T = T * this.X / 100;
                            successful_tested = 0;
                            positions_tested = 0;
                            number_of_temps++;
                        }
                    }

                }
            }

            DateTime Time2 = DateTime.Now;
            // Difference in days, hours, and minutes.
            TimeSpan ts = Time2 - Time1;
            // Difference in ticks.
            timesp = (int)ts.TotalMilliseconds;
            return A;
        }


        /// <summary>
        /// 产生小于oNo的随机整数
        /// </summary>
        /// <param name="fromNo">最小数</param>
        /// <param name="toNo">最大数</param>
        /// <returns>一个整数</returns>
        private  int GenerateRandomNumber(int MaxNo)
        {
            Random random = new Random();
            return random.Next(MaxNo);
        }

        /// <summary>
        /// 产生0-1之间的随机数
        /// </summary>
        /// <returns>0-1之间的随机数</returns>
        public double GenerateRandomFoat()
        {
            Random random = new Random();
            return random.NextDouble();
        }

        /// <summary>
        /// 回到原来的位置
        /// </summary>
        /// <param name="move_Obj"></param>
        /// <param name="cur_Pos"></param>
        private void Reture2OriginalPos(SDS_PolygonO move_Obj,State state)
        {
            int index=0;
            for(index=0;index<this.MapSDS.PolygonOObjs.Count ;index++)
            {
                if (this.MapSDS.PolygonOObjs[index] == move_Obj)
                    break;
            }
            int PosID = state.TrialPositionIDs[index];
            double dx = this.DispTemplate.TrialPosiList[PosID].Dx;
            double dy = this.DispTemplate.TrialPosiList[PosID].Dy;
            if (dx != 0 || dy != 0)
            {
                move_Obj.Translate((-1.0) * dx, (-1.0) * dy);
            }
        }
        /// <summary>
        /// 自动计算初始温度
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private double CalInitT(int N)
        {
            double a = this.Evaluate_InitState();
            double m = 0;
            double b = 0;
            int count = 0;
            if (a == 0)//或小于摸个阈值
                return 0;
            else
            {
                State cur_State = null;
                cur_State = this.State;
              
                int n = this.MapSDS.PolygonOObjs.Count;
                int TriPosiCount = this.DispTemplate.TrialPosiList.Count;
                bool isContinue = true;
                SDS_PolygonO move_Obj = null;
                int move_Pos = -1;
                List<Conflict> oNewConflict = null;//对象新的冲突
                List<Conflict> oCurConflict = null;//对象当前冲突
                int index = -1;//对象在数组中的位置
                double Dx = -1;
                double Dy = -1;
                double Dx0 = 0;
                double Dy0 = 0;
    
                int indexofObj = -1;
                int indexofPos = -1;
                SDS_PolygonO curObj;
                double dE = -1;
                for (int i = 0; i < 500;i++) 
                {
             
                    indexofObj = this.GenerateRandomNumber(n);
                    curObj = MapSDS.PolygonOObjs[indexofObj];
                    indexofPos = this.GenerateRandomNumber(TriPosiCount);
                    List<Conflict> oNewC = null;//对象新的冲突
                    List<Conflict> oCurC = null;//对象当前冲突
                    int id;
                    double dx;
                    double dy;
                    double dx0;
                    double dy0;
                    bool isInverted;
                    b = this.Evaluate_NewState(cur_State, curObj, indexofPos, out oCurC, out oNewC, out id, out dx, out dy, out dx0, out dy0, out isInverted);
                    if (isInverted == true)//三角形穿越
                        continue;
                    dE = b - 1;
                    if (dE < 0)
                    {
                        move_Obj = curObj;
                        move_Pos = indexofPos;
                        a = b;
                        oNewConflict = oNewC;
                        oCurConflict = oCurC;
                        index = id;
                        Dx = dx;
                        Dy = dy;
                        Dx0 = dx0;
                        Dy0 = dy0;
                        //Reture2OriginalPos(move_Obj, cur_State);
                        cur_State.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                    }
                    else
                    {
                        dE = Math.Abs(b - a);
                        m = m + dE;
                        count++;
                    }

                }
            }
            //还原状态;
            foreach (SDS_PolygonO move_Obj in this.MapSDS.PolygonOObjs)
            {
                this.Reture2OriginalPos(move_Obj, this.State);
            }
            return (m / count) / Math.Log(N);
        }
        /// <summary>
        /// 自动设置初始温度
        /// </summary>
        /// <param name="InitCN"></param>
        /// <returns></returns>
        public double AutoTDoDoAlgSimu_Annealing_Jones(out int InitCN)
        {
            InitCN = 0;
            this.V = this.CalInitT(3); ;
            if (V == 0)
                return -1;
            int timesp;
            return this.DoAlgSimu_Annealing_Jones(out InitCN,out timesp);
        }
    }
}
