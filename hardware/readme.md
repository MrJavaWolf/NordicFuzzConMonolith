
# Hexagonal LED Monolith – Technical Specifications

## 1. Overall Display

The LED monolith is a six-sided (hexagonal) structure, with LED panels covering all six sides.

The LED display is elevated approximately 50 cm above the floor, bringing the total height from floor level to the top of the display to approximately 2.04 meters.

| Specification | Details |
|---|---|
| Shape | Hexagon (6 sides) |
| Total Resolution | 768 × 512 pixels |
| LED Display Height | 1,536 mm (1.54 m) |
| Elevation Above Floor | Approx. 500 mm (50 cm) |
| Total Height from Floor | Approx. 2,036 mm (2.04 m) |
| Width per Side | 384 mm (38.4 cm) |
| Number of Sides | 6 |
| Total LED Panels | 96 |

## 2. Individual LED Panels

Each LED panel has the following specifications:

| Specification | Details |
|---|---|
| Resolution | 64 × 64 pixels |
| Physical Dimensions | 192 × 192 mm |
| Pixel Pitch | 3 mm |
| Panel Model | P3 Indoor 64×64 LED Display Module |

## 3. Display Configuration per Side

Each of the six sides consists of **2 LED panels horizontally and 8 LED panels vertically**.

| Specification | Details |
|---|---|
| Panels per Row | 2 |
| Panels per Column | 8 |
| Total Panels per Side | 16 |
| Resolution per Side | 128 × 512 pixels |
| Physical Width | 384 mm |
| Physical Display Height | 1,536 mm |

When looking at the display users can comfortable look at 2 sides at a time, and if they stand correctly, they can look at 3 sides at a time. Users are NOT able to see more then at most 3 sides at a time. 

## 4. Complete Hexagonal Display

With all six sides combined, the complete monolith has:

- **Total Resolution:** 768 × 512 pixels
- **Total Pixel Count:** 393,216 pixels
- **Total LED Panels:** 96 (16 per side × 6 sides)
- **Total Display Height:** 1,536 mm (1.54 m)
- **Display Elevation:** Approx. 500 mm (0.5 m) above the floor
- **Total Height from Floor:** Approx. 2,036 mm (2.04 m)
- **Total Display Width (unwrapped):** 2,304 mm (2.30 m)

## 5. Content & Video Mapping

For content creation, the six sides can be mapped as one continuous LED canvas.

**Full Canvas Resolution: 768 × 512 pixels**
Each side occupies a 128 × 512 pixel section of the overall canvas.
This allows content to flow seamlessly around all six sides of the monolith, creating a continuous 360° visual experience.

# Hardware

## 7. Hardware Overview

The LED monolith uses the following hardware:

- **LED Panels:** P3 Indoor 64×64 Pixel LED Display Modules
- **Receiver Cards:** Colorlight 5A-75B Receiver Cards (v8.2)

## 8. LED Matrix – P3 Indoor 64×64 Pixel LED Display Module

The monolith uses P3 indoor LED display modules with a resolution of 64 × 64 pixels and physical dimensions of 192 × 192 mm.

### Technical Specifications

| Specification | Details |
|---|---|
| Panel Type | P3 Indoor LED Display Module |
| Resolution | 64 × 64 pixels |
| Physical Dimensions | 192 × 192 mm |
| Total Panels | 96 |

### Documentation & Resources

- [LED Panel Image](panel.webp)
- [LED Panel Wiring Diagram](wiring.jpg)
- [LED Panel Technical Specifications (PDF)](Muen-P3%20indoor%20192x192mm%20LED%20module%20spec-20241217.pdf)
- [Purchase Link – AliExpress](https://www.aliexpress.com/item/1005004049950554.html)

## 9. Colorlight 5A-75B Receiver

The LED panels are controlled using **Colorlight 5A-75B Receiver Cards (v8.2)**.

### Documentation & Resources

- [Colorlight 5A-75B Receiver Specifications](Colorlight%205A-75B%20Receiver.avif)
- [Purchase Link – AliExpress](https://www.aliexpress.com/item/1005007011320683.html)

### 9.1 Firmware Compatibility Issue

The Colorlight 5A-75B Receiver Cards (v8.2) originally came with **PWM firmware version 11**.

This firmware was not compatible with our P3 LED panels and caused significant flickering and other display issues.

The solution was identified through the following resources:

- [AusChristmasLighting Forum – P5 Panels Flickering](https://auschristmaslighting.com/threads/p5-panels-flickering.15417/#post-143794)
- [YouTube – Firmware/Configuration Solution](https://www.youtube.com/watch?v=LcTKwyMmJec)

To get the receiver cards working correctly with the panels, two steps are required:

1. **Update the receiver card firmware**
2. **Configure the receiver cards using the correct panel configuration file**

### 9.2 Firmware Update

The receiver card firmware must be updated using **LEDUpgrade 4.0**.

**Software:**
[Download LEDUpgrade 4.0](https://en.colorlightinside.com/service/download/?cat=959)

#### Firmware Upgrade Procedure

Open LEDUpgrade 4.0 and navigate to:

`Upgrade firmware → Preset firmware... → normal-11.09`

Select the **normal-11.09** firmware preset and upgrade the receiver card.

This firmware resolved the flickering issues experienced with the original PWM firmware.

### 9.3 Receiver Card Configuration

After updating the firmware, the receiver cards must be configured to work with the P3 LED panels.

**Recommended Software:**
[LEDVISION v8.6](https://www.colorlight-led.com/colorlight-ledvision-8-6-download/)

Other versions of [LEDVISION v8.X](https://www.colorlight-led.com/colorlight-ledvision-download/) may also work.

**Important:** We were unable to get LEDVISION v9 working correctly with the Colorlight 5A-75B Receiver Cards. LEDVISION v8.6 is therefore recommended for this setup.

#### Required Configuration File

The receiver card configuration requires the following file:

`P3-64X64-32S-6124DJ+7262.rcvbp`

This file was provided by the LED panel seller and is located in the project folder.

The configuration file contains the panel-specific settings required for the receiver cards to communicate correctly with the LED modules.

**Important:** Use the seller-provided configuration file for these specific panels. Different LED panels may require different receiver card configurations, even if they have the same resolution and pixel pitch.

#### Problem: LEDVISION v9 Not Working

**Symptoms:**
- Unable to configure the Colorlight 5A-75B receiver cards correctly using LEDVISION v9.

**Solution:**
Use **LEDVISION v8.6** instead.

This version was used successfully with the Colorlight 5A-75B receiver cards in our setup.

