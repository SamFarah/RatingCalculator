using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;

namespace MythicPlanner.TagHelpers;


[HtmlTargetElement("input", Attributes = "asp-for")]
public class ImprovedInputTagHelper : InputTagHelper
{
    public ImprovedInputTagHelper(IHtmlGenerator generator) : base(generator) { }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        base.Process(context, output);
        var metaData = For.Metadata as DefaultModelMetadata;
        bool hasRequiredAttribute = metaData?.Attributes?.PropertyAttributes?.Any(i => i.GetType() == typeof(RequiredAttribute)) ?? false;
        if (hasRequiredAttribute)
        {
            output.Attributes.Add("required", null);
        }


        var rangeAttribute = metaData?.Attributes?.PropertyAttributes?.FirstOrDefault(i => i.GetType() == typeof(RangeAttribute));
        if (rangeAttribute != null)
        {
            var range = (RangeAttribute)rangeAttribute;
            output.Attributes.Add("min", range.Minimum);
            output.Attributes.Add("max", range.Maximum);
        }

    }
}