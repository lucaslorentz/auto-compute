using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public delegate ComputedUpdater ComputedUpdaterFactory(IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer, IProperty property);

public delegate Task<int> ComputedUpdater(DbContext dbContext);
