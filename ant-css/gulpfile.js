var gulp = require("gulp"),
  cleanCss = require("gulp-clean-css"),
  less = require("gulp-less");

var rename = require('gulp-rename');

gulp.task("default", function () {
  return gulp.src('index.less')
    .pipe(less({ javascriptEnabled: true }))
    .pipe(cleanCss({ compatibility: 'ie8' }))
    .pipe(rename('aims.css'))
    .pipe(gulp.dest('dest'));
});