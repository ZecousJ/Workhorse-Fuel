import AppKit

let sizes = [16, 32, 128, 256, 512]
let folder = URL(fileURLWithPath: CommandLine.arguments[1], isDirectory: true)
let dark = CGColor(red: 0.20, green: 0.17, blue: 0.18, alpha: 1)
let coral = CGColor(red: 1, green: 0.31, blue: 0.24, alpha: 1)

func icon(pixels: Int) -> Data {
    let context = CGContext(data: nil, width: pixels, height: pixels, bitsPerComponent: 8,
                            bytesPerRow: 0, space: CGColorSpace(name: CGColorSpace.sRGB)!,
                            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
    let scale = CGFloat(pixels) / 1024
    context.translateBy(x: 0, y: CGFloat(pixels))
    context.scaleBy(x: scale, y: -scale)

    let square = CGRect(x: 100, y: 100, width: 824, height: 824)
    let tile = CGPath(roundedRect: square, cornerWidth: 186, cornerHeight: 186, transform: nil)
    context.saveGState()
    context.setShadow(offset: CGSize(width: 0, height: -19), blur: 33,
                      color: CGColor(gray: 0, alpha: 0.31))
    context.addPath(tile)
    context.setFillColor(dark)
    context.fillPath()
    context.restoreGState()

    context.addPath(tile)
    context.setStrokeColor(CGColor(red: 0.40, green: 0.34, blue: 0.35, alpha: 1))
    context.setLineWidth(9)
    context.strokePath()

    let badge = CGPath(roundedRect: CGRect(x: 229, y: 229, width: 566, height: 566),
                       cornerWidth: 156, cornerHeight: 156, transform: nil)
    context.addPath(badge)
    context.setFillColor(coral)
    context.fillPath()

    context.translateBy(x: 296, y: 296)
    context.scaleBy(x: 18, y: 18)
    context.addPath(Brand.silhouette())
    context.setFillColor(CGColor(gray: 1, alpha: 1))
    context.fillPath(using: .evenOdd)

    let bitmap = NSBitmapImageRep(cgImage: context.makeImage()!)
    return bitmap.representation(using: .png, properties: [:])!
}

for size in sizes {
    try icon(pixels: size).write(to: folder.appendingPathComponent("icon_\(size)x\(size).png"))
    try icon(pixels: size * 2).write(to: folder.appendingPathComponent("icon_\(size)x\(size)@2x.png"))
}
