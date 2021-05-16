/// <binding AfterBuild='copyfiles' />
var gulp = require("gulp");

gulp.task("copyfiles", function () {
    return gulp.src(
        ["node_modules/bootstrap/**",
        "node_modules/jquery/**",
        "node_modules/jquery-validation/**",
        "node_modules/jquery-validation-unobtrusive/**",
        "node_modules/jquery-flexdatalist/**"
        ],
        { base: "node_modules" }
    ).pipe(gulp.dest("wwwroot/lib"));
});