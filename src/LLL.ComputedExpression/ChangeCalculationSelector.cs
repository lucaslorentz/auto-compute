namespace LLL.ComputedExpression;

public delegate IChangeCalculation<TValue, TResult> ChangeCalculationSelector<TValue, TResult>(ChangeCalculations<TValue> changeCalculations);