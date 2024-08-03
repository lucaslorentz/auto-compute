namespace LLL.AutoCompute;

public delegate IChangeCalculation<TValue, TChange> ChangeCalculationSelector<TValue, TChange>(IChangeCalculations<TValue> changeCalculations);