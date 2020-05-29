This repo contains code for the DRAL(Digital Representation of Attention Labeler)


# DRAL

## Requirements
- Windows machine with WPF
- .Net Core 3.1
- Visual Studio 2019
## Usage
- Load labels
- Load an image
- Move the cursor to display the image
- Save
## Shortcuts 
When started, this labeller uses the following shortcuts:

| Shortcut | Action                                               | Alternate shortcut |
| -------- | ---------------------------------------------------- | ------------------ |
| CTRL-B   | Load next image                                      | CTRL-Y             |
| CTRL-N   | Save then load next image                            | CTRL-S             |
| CTRL-O   | Open image                                           |                    |
| CTRL-R   | Reset the labelling for the current image            |                    |
| CTRL-S   | Save then load next image			                  | CTRL-N             |
| CTRL-W   | Load next until find an unlabelled one               |                    |
| CTRL-X   | Load previous                                        |                    |
| CTRL-Y   | Load next image                                      | CTRL-B             |
| CTRL-Z   | Cancel the current image labelling and load previous |                    |

## WIP

Allow unlabelled data to be labelled