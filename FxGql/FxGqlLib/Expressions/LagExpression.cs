using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FxGqlLib
{
    static class LagExpressionFactory
    {
        public static StateExpression<IData, IData[], IData> Create(IExpression arg)
        {
            return Create(arg, 1);
        }

        public static StateExpression<IData, IData[], IData> Create(IExpression arg, DataInteger lagOffset)
        {
            return Create(arg, lagOffset, DataTypeUtil.GetDefaultFromDataType(arg.GetResultType()));
        }

        public static StateExpression<IData, IData[], IData> Create(IExpression arg, DataInteger lagOffset, IData argDefault)
        {
            //return new StateExpression<IData, Tuple<IData, IData>, IData>(
            //    (a) => new Tuple<IData, IData>(DataTypeUtil.GetDefaultFromDataType(arg.GetResultType()), a),
            //    delegate (Tuple<IData, IData> s, IData a) { s = new Tuple<IData, IData>(s.Item2, a); return s; },
            //    (s) => s.Item1,
            //    ConvertExpression.CreateData(arg)
            //    );

            return new StateExpression<IData, IData[], IData>(
                delegate (IData a)
                {
                    var s = new IData[lagOffset + 1];
                    for (int i = 0; i < lagOffset; i++)
                        s[i] = argDefault;
                    s[lagOffset] = a;
                    return s;
                },
                delegate (IData[] s, IData a)
                {
                    for (int i = 0; i < lagOffset; i++)
                        s[i] = s[i + 1];
                    s[lagOffset] = a;
                    return s;
                },
                (s) => s[0],
                ConvertExpression.CreateData(arg)
                );
        }
    }
}
