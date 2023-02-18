# Specify API

The Specify client submits a single POST request to `/upload.php` containing the generated JSON. This requires a form body type of `application/json`, and is sent to a valid [Specified](https://github.com/Spec-ify/specified) endpoint. If the response returns a success code, the `location` header will contain the URL of the generated report, where the client then opens the page in the default web browser.

When prompted, Specify will also submit a POST request to [`dumpload.spec-ify.com`](https://dumpload.spec-ify.com) with a zip file containing minidumps. The code for the dump upload site is from [single_php_host by Rouji](https://github.com/Rouji/single_php_filehost)