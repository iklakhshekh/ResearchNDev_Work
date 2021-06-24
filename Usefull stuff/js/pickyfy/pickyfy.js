(function($) {
    $.fn.pickyfy = function(options, arg) {
        var settings = $.extend({
            allowDuplicate: false,
            width: "100%",
        }, options);
        if (options && typeof(options) == 'string') {
            if (options == 'getData') {
                return getSelectedData(this.selector);
            } else if (options == 'setData') {
                return setSelectedData(this.selector, arg);
            }
            else if (options == 'clearData') {
                return clearData(this.selector);
            }
        }
        function clearData(selector) {
            chosenContainerSelector = $(selector).next(".chosen-container");
            chosenContainerSelector.find(".search-choice").remove();
            return;
        }
        function setSelectedData(selector, options) {
            chosenContainerSelector = $(selector).next(".chosen-container");
            chosenContainerSelector.find(".search-choice").remove();
            for (var i in options) {
                var li = '<li class="search-choice"  data-option-id="' + options[i].id + '" style="position: relative; left: 0px; top: 0px;">' +
                    '<span>' + options[i].text + '</span>' +
                    '<a class="search-choice-close"></a>' +
                    '</li>';
                chosenContainerSelector.find("li.search-field").before(li);
            }
            return;
        }
        function getSelectedData(selector) {
            var options = [];
            chosenContainerSelector = $(selector).next(".chosen-container");
            var chosenSearchChoiceSelector = chosenContainerSelector.find(".search-choice");
            $(chosenSearchChoiceSelector).each(function() {
                options.push({
                    id: $(this).attr("data-option-id"),
                    text: $(this).children("span").text()
                });
            })
            return options;
        }
        var select = this.selector;
        var chosenContainer = '<div class="chosen-container chosen-container-multi" title="" style="width:' + settings.width + ' ;">' +
            '<ul class="chosen-choices ui-sortable">' +
            '<li class="search-field">' +
            '<input class="chosen-search-input" autocomplete="off" style="width: 50px;"  type="text">' +
            '</li>' +
            '</ul>' +
            '<div class="chosen-drop">' +
            '<ul class="chosen-results">' +
            '</ul>' +
            '</div>' +
            '</div>';
        //creating chosen container for select                           
        $(chosenContainer).insertAfter($(select));
        //creating chosen drop option menu by select options
        var _currentOptG = "";
        $(select + " option").each(function() {
            var optG = "",
                optValue = "",
                optGLi = "",
                optionLi = "",
                optText = "";
            optText = $(this).text();
            optValue = $(this).val();
            if ($(this).parent('optgroup').length > 0) {
                optG = $(this).parent("optgroup").attr("label");
                optGLi = '<li class="group-result">' + optG + '</li>';
                if (_currentOptG != optG) {
                    $(select).next(".chosen-container").find(".chosen-results").append(optGLi);
                    _currentOptG = optG;
                }
            }
            optionLi = '<li class="group-option active-result" data-option-id="' + optValue + '" >' + optText + '</li>';
            $(select).next(".chosen-container").find(".chosen-results").append(optionLi);
        });
        //selectors
        var chosenContainerSelector = $(select).next(".chosen-container");
        var activeResultSelector = chosenContainerSelector.find(".chosen-results li.active-result");
        var chosenDropSelector = chosenContainerSelector.find(".chosen-drop");
        var chosenResultsSelector = chosenDropSelector.find(".chosen-results");
        var optionRemoveSelector = chosenContainerSelector.find(".search-choice-close");
        var chosenSearchInputSelector = chosenContainerSelector.find(".chosen-search-input");
        //sortable ul li
        $(select).next(".chosen-container").find("ul.chosen-choices").sortable({
            containment: 'parent',
            update: function() {
                var searchField = $(this).find("li.search-field");
                $(this).find("li.search-field").remove();
                $(searchField).appendTo($(this));
                chosenSearchInputSelector.keypress(function(e) {
                    bindKepress(e);
                });
                 // search through the option by keyword
                chosenSearchInputSelector.keyup(function(e) {
                    bindKeyup(e);
                });

                // remove previous tag by backspace
                chosenSearchInputSelector.keydown(function(e) {
                    bindKeydown(e);
                });
            }
        });
        //highlighting option on hover
        activeResultSelector.hover(function() {
            $(this).addClass("highlighted")
        }, function() {
            $(this).removeClass("highlighted")
        });
        //showing the option menu
        chosenContainerSelector.click(function() {

            $(this).find("ul.chosen-results  li.active-result").show();
            chosenDropSelector.show();
            chosenSearchInputSelector.focus()
        });
        //hiding the option menu if click outside the option menu
        $('html').click(function() {
            chosenDropSelector.hide();
        });
        //check duplicate tag
        function checkDuplicateTag(text) {
            text = $.trim(text);
            if (!settings.allowDuplicate) {
                var options = getSelectedData(select).map(function(value) {
                    return value.text.toLowerCase();
                });
                if (options.indexOf(text.toLowerCase()) > -1) {
                    chosenContainerSelector.find(".search-choice").filter(function(a, b) {
                        if ($(b).text().toLowerCase() == text.toLowerCase()) {
                            $(this).addClass('duplicateTag').delay(1000).queue(function() {
                                $(this).removeClass('duplicateTag');
                                $(this).dequeue();
                            })
                        }
                    })
                    return false;
                }
            }
            return true;
        }
        //add tags
        activeResultSelector.click(function() {
            var selectedOption = $.trim($(this).text());
            var selectedValue = $.trim((this).attr("data-option-id"));
            if (selectedOption!="" && checkDuplicateTag(selectedOption)) {
                var li = '<li class="search-choice" data-option-id="' + selectedValue + '" style="position: relative; left: 0px; top: 0px;">' +
                    '<span>' + selectedOption + '</span>' +
                    '<a class="search-choice-close"></a>' +
                    '</li>';
                chosenContainerSelector.find("li.search-field").before(li);
                var selectedOptions = getSelectedData(select);
                for (var i in selectedOptions) {
                    var optionId = selectedOptions[i].id;
                    $(select).find("option[value='" + optionId + "']").prop("selected", "selected");
                }
            }
        });
        //remove tags 
        $(document).on('click', optionRemoveSelector, function() {
            var optionId = $(this).parent("li").attr("data-option-id");
            $(this).parent("li").remove();
            $(select).find("option[value='" + optionId + "']").removeAttr("selected");
        });
        chosenContainerSelector.click(function(event) {
            if ($(event.target).hasClass('search-choice-close')) {
                var optionId = $(event.target).parent("li").attr("data-option-id");
                var customTag = $(event.target).parent("li").attr("data-custom-tag");
                $(event.target).parent("li").remove();
                if (customTag == "true")
                    $(select).find("option[value='" + optionId + "']").remove();
                else
                    $(select).find("option[value='" + optionId + "']").removeAttr("selected");
            }
            if ($(event.target).hasClass('active-result')) {
                chosenDropSelector.hide();
            }
            var isVisible = chosenDropSelector.parent().find("div.chosen-drop").is(":visible");
            if (isVisible) {
                $('.chosen-drop').not(chosenDropSelector).hide()
            }
            event.stopPropagation();
        });
        chosenDropSelector.click(function(event) {
            if ($(event.target).hasClass('active-result')) {
                chosenDropSelector.hide();
            }
            event.stopPropagation();
        });
        //add new tag on enter key/ remove tag on backspace key
        chosenSearchInputSelector.keypress(function(e) {
            bindKepress(e);
        });
        // search through the option by keyword
        chosenSearchInputSelector.keyup(function(e) {
            bindKeyup(e);
        });

        // remove previous tag by backspace
        chosenSearchInputSelector.keydown(function(e) {
            bindKeydown(e);
        });
        function bindKeydown(e) {
              if (e.which === 8) {
                if (e.target.value.length == 0) {
                    var optionId = chosenSearchInputSelector.parent("li").prev("li.search-choice").attr("data-option-id");
                    var customTag = chosenSearchInputSelector.parent("li").prev("li.search-choice").attr("data-custom-tag");
                    chosenSearchInputSelector.parent("li").prev("li.search-choice").remove();
                    if (customTag == "true")
                        $(select).find("option[value='" + optionId + "']").remove();
                    else
                        $(select).find("option[value='" + optionId + "']").removeAttr("selected");
                }

            } 
        }
        function bindKeyup(e) {
                $(e.target).parent().parent().parent().find(".chosen-drop").hide();
                var inputValue = e.target.value.toLowerCase();
                if (inputValue.length > 0) {
                    $(activeResultSelector).each(function() {

                        if ($(this).html().toLowerCase().indexOf(inputValue) == -1) {
                            $(this).hide();
                        } else {
                            $(this).show();
                        }
                    });
                    $(e.target).parent().parent().parent().find(".chosen-drop").show();
                } else {
                    $(activeResultSelector).show();
                    $(e.target).parent().parent().parent().find(".chosen-drop").show();
                }
        }
        function bindKepress(e) {
            if (e.which === 13 && chosenSearchInputSelector.val().length > 0) {
                if ($.trim(chosenSearchInputSelector.val())!="" && checkDuplicateTag(chosenSearchInputSelector.val())) {
                    var lastOptionId = parseInt($(select).find("option:last").attr("value")) + 1;
                    var li = '<li class="search-choice" data-custom-tag="true" data-option-id="' + lastOptionId + '" style="position: relative; left: 0px; top: 0px;">' +
                        '<span>' + chosenSearchInputSelector.val() + '</span>' +
                        '<a class="search-choice-close"></a>' +
                        '</li>';
                    if ($(select).find("option[value='" + chosenSearchInputSelector.val() + "']").length > 0 && chosenContainerSelector.find(".search-choice").length == 0) {
                        $(select).find("option[value='" + chosenSearchInputSelector.val() + "']").remove();
                    }
                    $(select).append("<option value='" + lastOptionId + "'>" + chosenSearchInputSelector.val() + "</option>")
                    $(select).find("option[value='" + lastOptionId + "']").prop("selected", "selected");
                    chosenContainerSelector.find("li.search-field").before(li);
                    chosenSearchInputSelector.val('');
                    chosenDropSelector.hide();
                }
            }

        }

    };

}(jQuery));