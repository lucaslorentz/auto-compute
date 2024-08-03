using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public delegate ComputedUpdater ComputedUpdaterFactory(IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer, IProperty property);

public delegate Task<int> ComputedUpdater(DbContext dbContext);
