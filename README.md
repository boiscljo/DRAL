This repo contains code for the DRAL(Digital Representation of Attention Labeler)

# DRAL

## Requirements

* . Net Core 3.1[Not needed for releases except portable]
* Visual Studio 2019 for compiling[Not needed for releases]

## Usage

Default mode is retag

### Start

| Mode     | CLI            |
| -------- | -------------- |
| Tagger   | ./DRAL --new   |
| Retagger | ./DRAL --retag |

### Retagger

* Load labels
* Load an image
* Move the cursor to display the image
* Save

### Tagger

* Load an image
* Move the cursor to display the image
* Save
* Wait for X-means
* Tag boxes
* Save

## Command line interface

| Argument      | Explanation                  | Alternate |
| ------------- | ---------------------------- | --------- |
| --help        | Show help                    | -h        |
| --verbose     | Starts in verbose mode       | -v        |
| --no-window   | Start in CLI                 | --cli     |
| --new         | Start tagger                 | -n        |
| --retag       | Start retagger(default mode) | -r        |
| --fix-dataset | Fix dataset                  | --fix, -f |

__Fixing dataset is mainly for missing maps or running X-means if "Generate now" was unchecked for speed__
## Shortcuts 

### Tagger 1st window & Retagger

When started, this labeller uses the following shortcuts:

| Shortcut | Action                                               | Alternate shortcut |
| -------- | ---------------------------------------------------- | ------------------ |
| CTRL-B   | Load next image                                      | CTRL-Y             |
| CTRL-N   | Save then load next image                            | CTRL-S             |
| CTRL-O   | Open image                                           |                    |
| CTRL-R   | Reset the labelling for the current image            |                    |
| CTRL-S   | Save then load next image                            | CTRL-N             |
| CTRL-W   | Load next until find an unlabelled one               |                    |
| CTRL-X   | Load previous                                        |                    |
| CTRL-Y   | Load next image                                      | CTRL-B             |
| CTRL-Z   | Cancel the current image labelling and load previous |                    |

### Tagger 2nd window

| Shortcut | Action              | Alternate shortcut |
| -------- | ------------------- | ------------------ |
| CTRL-S   | Save and go to next | Ctrl-B             |
| CTRL-X   | Cancel              |                    |

## WIP
