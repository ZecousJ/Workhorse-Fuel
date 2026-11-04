import CoreGraphics

enum Brand {
    static func silhouette() -> CGPath {
        let p = CGMutablePath()
        p.move(to: CGPoint(x: 2, y: 2.6))
        p.addQuadCurve(to: CGPoint(x: 8.1, y: 7), control: CGPoint(x: 3.4, y: 6.6))
        p.addQuadCurve(to: CGPoint(x: 15.9, y: 7), control: CGPoint(x: 12, y: 4.9))
        p.addQuadCurve(to: CGPoint(x: 22, y: 2.6), control: CGPoint(x: 20.6, y: 6.6))
        p.addCurve(to: CGPoint(x: 18.7, y: 12.2), control1: CGPoint(x: 24, y: 7.8), control2: CGPoint(x: 21.6, y: 10.9))
        p.addLine(to: CGPoint(x: 17.6, y: 16.8))
        p.addQuadCurve(to: CGPoint(x: 12, y: 22), control: CGPoint(x: 16.4, y: 22))
        p.addQuadCurve(to: CGPoint(x: 6.4, y: 16.8), control: CGPoint(x: 7.6, y: 22))
        p.addLine(to: CGPoint(x: 5.3, y: 12.2))
        p.addCurve(to: CGPoint(x: 2, y: 2.6), control1: CGPoint(x: 2.4, y: 10.9), control2: CGPoint(x: 0, y: 7.8))
        p.closeSubpath()
        p.move(to: CGPoint(x: 13.7, y: 7.6))
        p.addLines(between: [CGPoint(x: 8.1, y: 14.1), CGPoint(x: 11.5, y: 14.1),
                             CGPoint(x: 10.4, y: 19.6), CGPoint(x: 16.3, y: 12.5),
                             CGPoint(x: 12.9, y: 12.5), CGPoint(x: 13.7, y: 7.6)])
        p.closeSubpath()
        return p
    }
}
