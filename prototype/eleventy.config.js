export default function (eleventyConfig) {
  eleventyConfig.addPassthroughCopy("src/css");
  eleventyConfig.addPassthroughCopy("src/js");
  eleventyConfig.addPassthroughCopy("src/img");
  eleventyConfig.addPassthroughCopy({
    "node_modules/@fontsource/nunito/files/nunito-latin-400-normal.woff2": "css/fonts/nunito-latin-400-normal.woff2",
    "node_modules/@fontsource/nunito/files/nunito-latin-700-normal.woff2": "css/fonts/nunito-latin-700-normal.woff2",
    "node_modules/@fontsource/baloo-2/files/baloo-2-latin-700-normal.woff2": "css/fonts/baloo-2-latin-700-normal.woff2",
  });

  return {
    dir: { input: "src", output: "_site" },
    markdownTemplateEngine: "njk",
    htmlTemplateEngine: "njk",
  };
}
