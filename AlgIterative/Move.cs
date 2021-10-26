using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgIterative
{
    /// <summary>
    /// 移动
    /// </summary>
    public class Move
    {
        public int move_Obj = -1;//移动对象
        public int move_Pos = -1;//移动位置
        public double Cost = -1;//目标函数值
        public Move(int moveobj, int movepos, double cost)
        {
            move_Obj = moveobj;
            move_Pos = movepos;
            Cost = cost;
        }
        /// <summary>
        /// 按Cost的大小排序
        /// </summary>
        public static void SortMoves(List<Move> moves)
        {
            moves.Sort(Compare);
        }

        /// <summary>
        /// 比较函数
        /// </summary>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <returns></returns>
        public static int Compare(Move i1, Move i2)
        {
            if (i1.Cost > i2.Cost) return 1;
            if (i1.Cost < i2.Cost) return -1;
            return 0;
        }
        /// <summary>
        /// 查找并获取一个Move对象
        /// </summary>
        /// <param name="moves">move</param>
        /// <returns></returns>
        public Move GetMove(List<Move> moves)
        {

            foreach (Move curMove in moves)
            {
                if (curMove.move_Obj == this.move_Obj && curMove.move_Pos == this.move_Pos)
                    return curMove;
            }
            return this;
        }

    }
}
