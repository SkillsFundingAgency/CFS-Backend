$(window).on('load', function () {
    $.get("Pages/IndexTop.html", function (data) {
        $('div.swagger-ui').prepend(data);
    });
});