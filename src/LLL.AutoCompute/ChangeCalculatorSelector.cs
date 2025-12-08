namespace LLL.AutoCompute;

public delegate IChangeCalculator<TValue, TChange> ChangeCalculatorSelector<TValue, TChange>(IChangeCalculators<TValue> changeCalculations);
