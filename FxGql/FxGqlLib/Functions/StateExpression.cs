using System;

namespace FxGqlLib
{
    public class StateExpression<T, S, R> : Expression<R>
        where T : IData
        where R : IData
    {
        readonly Func<T, S> init;
        readonly Func<S, T, S> processor;
        readonly Func<S, R> calculate;
        readonly Expression<T> arg;

        public StateExpression(Func<T, S> init, Func<S, T, S> processor, Func<S, R> calculate, Expression<T> arg)
            : this(init, processor, calculate, arg, false)
        {
        }

        public StateExpression(Func<T, S> init, Func<S, T, S> processor, Func<S, R> calculate, Expression<T> arg,
                                      bool runProcessorForFirst)
        {
            if (runProcessorForFirst)
            {
                this.init = delegate (T a) {
                    return processor(init(a), a);
                };
            }
            else
            {
                this.init = init;
            }
            this.processor = processor;
            this.calculate = calculate;
            this.arg = arg;
        }

        #region implemented abstract members of FxGqlLib.Expression[T]

        public override R Evaluate(GqlQueryState gqlQueryState)
        {
            throw new NotSupportedException();
        }

        public override Type GetResultType()
        {
            return arg.GetResultType();
        }

        public override bool IsAggregated()
        {
            return false;
        }

        public override bool HasState()
        {
            return true;
        }

        public override void Process(StateBin state, GqlQueryState gqlQueryState)
        {
            S stateValue;
            T t = arg.Evaluate(gqlQueryState);
            if (!state.GetState<S>(this, out stateValue))
                stateValue = init(t);
            else
                stateValue = processor(stateValue, t);
            state.SetState(this, stateValue);
        }

        public override IData ProcessCalculate(StateBin state)
        {
            S stateValue;
            if (!state.GetState<S>(this, out stateValue))
                throw new InvalidOperationException("Aggregation state not found"); // TODO: Aggregation without values
            return calculate(stateValue);
        }

        #endregion

    }
}

