using System;

    public class SelectListItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string YourCustomTag { get; set; }
    }

    public static class CustomDropdownlist
    {
        
        /// <summary>
        /// Custom Dropdownlist (if you need to add more than two properties (id and value))
        /// it will help you if you want to add data-attr in your option
        /// eg. <option data-customattr='some value'>
        /// </summary>
        /// <param name="helper">the htmlhelper object</param>
        /// <param name="id"></param>
        /// <param name="items"></param>
        /// <param name="optionalLabel"></param>
        /// <param name="idSelected"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString DropDownListForWithTag(this HtmlHelper helper, string id, List<SelectListItem> items,string optionalLabel, string selectedListItem, object htmlAttributes)
        {
            var select = new TagBuilder("select");
            select.GenerateId(id);
            select.MergeAttribute("name", id);
            select.MergeAttributes(new RouteValueDictionary(htmlAttributes));
            TagBuilder headerOption = new TagBuilder("option");
            headerOption.MergeAttribute("value", "");
            headerOption.InnerHtml = optionalLabel;
            select.InnerHtml += headerOption;
            

            foreach (var item in items)
            {
                TagBuilder option = new TagBuilder("option");
                option.MergeAttribute("value", item.Id.ToString());
                //  option.MergeAttribute("data-customattr", "your custom value");
                if (selectedListItem == item.Id) option.MergeAttribute("selected", "selected");
                option.InnerHtml = item.Name;
                select.InnerHtml += option.ToString();
            }

            return new MvcHtmlString(select.ToString());
        }
    }


