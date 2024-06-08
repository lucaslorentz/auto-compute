namespace LLL.ComputedExpression;

public delegate IChangeCalculation<TValue, TChange> ChangeCalculationSelector<TValue, TChange>(IChangeCalculations<TValue> changeCalculations);