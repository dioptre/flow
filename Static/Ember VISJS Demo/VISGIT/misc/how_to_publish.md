# How to publish vis.js

This document describes how to publish vis.js.


## Build

- Change the version number of the library in both `package.json` and `bower.json`.
- Open `HISTORY.md`, write down the changes, version number, and release date.
- Build the library by running:

        npm update
        npm run build

This generates the vis.js library in the folder `./dist`.


## Test

- Test the library:

        npm test

- Open some of the example in your browser and visually check if it works as expected.


## Commit

- Commit the changes to the `develop` branch.
- Merge the `develop` branch into the `master` branch.
- Push the branches to github
- Create a version tag (with the new version number) and push it to github:

        git tag v0.3.0
        git push --tags


## Publish

- Publish at npm:

        npm publish

- Test the published library:
  - Go to a temp directory
  - Install the library from npm:

          npm install vis

    Verify if it installs the just released version, and verify if it works.

  - Install the library via bower:

          bower install vis

    Verify if it installs the just released version, and verify if it works.


- Publish the library at cdnjs.org
  - clone the cdnjs project
  - pull changes: `git pull upstream`
  - add the new version of the library under /ajax/libs/vis/
    - add new folder /x.y.z/ with the new library
    - update the version number in package.json
  - test the library by running `npm test`
  - then do a pull request with as title "[author] Update vis.js to x.y.z"
    (with correct version).



## Update website

- Copy the `dist` folder from the `master` branch to the `github-pages` branch.
- Copy the `examples` folder from the `master` branch to the `github-pages` branch.
- Copy the `docs` folder from the `master` branch to the `github-pages` branch.
- Create a packaged version of vis.js. Go to the `master` branch and run:

        zip vis.zip dist docs examples README.md HISTORY.md LICENSE NOTICE -r

- Move the created zip file `vis.zip` to the `download` folder in the
  `github-pages` branch. TODO: this should be automated.

- Go to the `github-pages` branch and run the following script:

        node updateversion.js

- Commit the changes in the `gh-pages` branch.


## Prepare next version

- Switch to the `develop` branch.
- Change version numbers in `package.json` and `bower.json` to a snapshot
  version like `0.4.0-SNAPSHOT`.
