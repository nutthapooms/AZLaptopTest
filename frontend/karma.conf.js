// Karma configuration file, see link for more information

// https://karma-runner.github.io/1.0/config/configuration-file.html

module.exports = function (config) {
    config.set({
        basePath: "",
        frameworks: ["jasmine", "@angular-devkit/build-angular"],
        plugins: [
            require("karma-jasmine"),
            require("@chiragrupani/karma-chromium-edge-launcher"),
            require("karma-chrome-launcher"),
            require("karma-spec-reporter"),
            require("karma-jasmine-html-reporter"),
            require("karma-sonarqube-reporter"),
            require("karma-coverage"),
            require("@angular-devkit/build-angular/plugins/karma"),
        ],
        client: {
            captureConsole: false,
            jasmine: {
                // you can add configuration options for Jasmine here
                // the possible options are listed at https://jasmine.github.io/api/edge/Configuration.html
                // for example, you can disable the random execution with `random: false`
                // or set a specific seed with `seed: 4321`
            },
            clearContext: false, // leave Jasmine Spec Runner output visible in browser
        },
        jasmineHtmlReporter: {
            suppressAll: true, // removes the duplicated traces
        },
        coverageReporter: {
            dir: require("path").join(__dirname, "./coverage/integrateduw"),
            subdir: ".",
            reporters: [
                { type: "lcov", subdir: "lcov-report" },
                { type: "html", subdir: "html-report" },
                { type: "text-summary" },
            ],
        },
        reporters: ["progress", "kjhtml", "coverage", "spec", "sonarqube"],
        port: 9876,
        colors: true,
        logLevel: config.LOG_ERROR,
        singleRun: false,
        restartOnFileChange: true,
        autoWatch: true,
        browsers: ["EdgeHeadlessNoSandbox"],
        customLaunchers: {
            EdgeHeadlessNoSandbox: {
                base: "EdgeHeadless",
                flags: [
                    "--no-sandbox",
                    "--disable-gpu",
                    "--remote-debugging-port=9222",
                    "--disable-site-isolation-trials",
                ],
            },
            ChromeHeadlessNoSandbox: {
                base: "ChromeHeadless",
                flags: [
                    "--no-sandbox",
                    "--disable-gpu",
                    "--disable-web-security",
                    "--remote-debugging-port=9222",
                    "--disable-site-isolation-trials",
                ],
            },
        },
    });
};
