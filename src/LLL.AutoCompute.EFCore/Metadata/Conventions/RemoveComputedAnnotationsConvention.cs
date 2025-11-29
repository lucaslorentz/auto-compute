using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace LLL.AutoCompute.EFCore.Metadata.Conventions;

class RemoveComputedAnnotationsConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        if (!EF.IsDesignTime)
            return;

        var annotatables = modelBuilder.Metadata.GetEntityTypes()
            .SelectMany(e => new IConventionAnnotatable[] { e }.Concat(e.GetMembers().OfType<IConventionAnnotatable>()))
            .Concat([modelBuilder.Metadata])
            .ToArray();

        foreach (var annotatable in annotatables)
        {
            var computedAnnotations = annotatable
                .GetAnnotations()
                .Where(a => a.Name.StartsWith(AutoComputeAnnotationNames.TempPrefix))
                .ToArray();

            if (computedAnnotations.Length > 0)
            {
                foreach (var annotation in computedAnnotations)
                    annotatable.RemoveAnnotation(annotation.Name);
            }
        }
    }
}
