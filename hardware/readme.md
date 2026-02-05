# Hardware

- Colorlight 5A-75B Receiver
- LED Matrix - P3 Indoor 64*64 pixels LED Display Module

## LED Matrix - P3 Indoor 64*64 pixels LED Display Module

See the panel [here](panel.webp) and [here](wiring.jpg). The technical specifications for the panels is [here](Muen-P3%20indoor%20192x192mm%20LED%20module%20spec-20241217.pdf).
Brought from [here](https://www.aliexpress.com/item/1005004049950554.html?spm=a2g0o.order_list.order_list_main.29.88bf1802Ay4k2V)

## Colorlight 5A-75B Receiver

The technical specifications for the receiver is [here](Colorlight%205A-75B%20Receiver.avif).
Brought from [here](https://www.aliexpress.com/item/1005007011320683.html?spm=a2g0o.order_list.order_list_main.17.88bf1802Ay4k2V#nav-specification).

The 5A-75B Colorlight Receiver card v 8.2 came with version 11 PWM which did not work with the panels at all. The panels had odd flickering.

The solution was found [here](https://auschristmaslighting.com/threads/p5-panels-flickering.15417/#post-143794) and [here](https://www.youtube.com/watch?v=LcTKwyMmJec).

To get them to work with the panels you need to:

1. Update the 5A-75B Colorlight Receiver card firmware
2. Configure the 5A-75B Colorlight Receiver card

### Firmware

Use [LEDUpgrade 4.0](https://en.colorlightinside.com/service/download/?cat=959) to upgrade the 5A-75B Colorlight Receiver card:

```text
Upgrade firmware --> Preset firmware... --> normal-11.09
```

### Configure

To configure the receiver cards use [LEDVISION v8.6](https://www.colorlight-led.com/colorlight-ledvision-8-6-download/). Other [LEDVISION v8.X](https://www.colorlight-led.com/colorlight-ledvision-download/) may work.

I did not manage to get `LEDVISION v9` to work with the 5A-75B Colorlight Receiver cards.

Doing the configuration phase you need the `P3-64X64-32S-6124DJ+7262.rcvbp` file located in this folder. We received that file from the panel seller.

You need to configure:

- Number of pixels
- Number of receiver cards
- And more... 
