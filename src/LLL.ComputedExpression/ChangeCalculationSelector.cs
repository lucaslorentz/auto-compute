namespace LLL.ComputedExpression;

public delegate IChangeCalculation<TValue, TResult> ChangeCalculationSelector<TValue, TResult>(IChangeCalculations<TValue> changeCalculations);