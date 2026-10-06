import argparse
import json
import sys
from pathlib import Path


#在原始特效层中按相连像素区分星光与散点，原 RGB 和星光 Alpha 保持不变。
def split_connected_starlight(rgba, np):
    remaining = np.array(rgba[:, :, 3] > 0, dtype='uint8')
    height, width = remaining.shape
    components = []
    for yy, xx in zip(*np.nonzero(remaining)):
        if not remaining[yy, xx]:
            continue
        remaining[yy, xx] = 0
        stack, points = [(int(xx), int(yy))], []
        while stack:
            x, y = stack.pop()
            points.append((x, y))
            for ny in range(max(0, y - 1), min(height, y + 2)):
                for nx in range(max(0, x - 1), min(width, x + 2)):
                    if remaining[ny, nx]:
                        remaining[ny, nx] = 0
                        stack.append((nx, ny))
        components.append(points)
    if not components:
        raise ValueError('PSD 的特效层没有可见像素。')
    components.sort(key=len, reverse=True)
    points = np.array(components[0])
    #已核对原图：连续十字星光横贯整张特效层，散点均为较小独立区域。
    if points[:, 0].max() - points[:, 0].min() < width * 0.9:
        raise ValueError('最大连续区域不是横贯画布的星光，停止自动分离。')
    keep = np.zeros((height, width), dtype=bool)
    keep[points[:, 1], points[:, 0]] = True
    return keep, len(components) - 1


#另存分层 PSD 并按既有统一画布导出，不覆盖原始 PSD。
def main():
    parser = argparse.ArgumentParser(description='从刀兵 PSD 特效层分离静态火星。')
    parser.add_argument('--psd', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--texture', required=True, type=Path)
    parser.add_argument('--layout', required=True, type=Path)
    parser.add_argument('--dependencies', type=Path)
    args = parser.parse_args()
    if args.psd.resolve() == args.output.resolve():
        raise ValueError('输出 PSD 必须另存，不能覆盖输入原稿。')
    if args.dependencies:
        sys.path.insert(0, str(args.dependencies))
    import numpy as np
    from PIL import Image
    from psd_tools import PSDImage
    from psd_tools.api.layers import PixelLayer

    psd = PSDImage.open(args.psd)
    group = psd[1][0]
    glow = next(layer for layer in group if layer.name == '特效')
    if glow.has_mask():
        raise ValueError('原特效层已有蒙版，需要先检查蒙版，不能覆盖。')
    original = np.array(glow.topil().convert('RGBA'))
    keep, removed_count = split_connected_starlight(original, np)
    cleaned = original.copy()
    cleaned[~keep, 3] = 0
    removed = original.copy()
    removed[keep, 3] = 0
    mask = Image.fromarray(np.where(keep, 255, 0).astype('uint8'))
    glow.create_mask(mask, top=glow.top, left=glow.left)
    separated = PixelLayer.frompil(Image.fromarray(removed), group,
        name='静态火星（停绘，原像素保留）', top=glow.top, left=glow.left)
    separated.blend_mode = glow.blend_mode
    separated.opacity = glow.opacity
    separated.visible = False
    group.insert(list(group).index(glow) + 1, separated)
    dagger_sparks = next(layer for layer in group if layer.name == '图层 2')
    dagger_sparks.name = '匕首静态火星（停绘）'
    dagger_sparks.visible = False
    args.output.parent.mkdir(parents=True, exist_ok=True)
    psd.save(args.output, encoding='utf-8')

    layout = json.loads(args.layout.read_text(encoding='utf-8'))['统一画布']['Pawn']
    left, top, right, bottom = layout['source_rect']
    canvas = Image.new('RGBA', (right - left, bottom - top))
    canvas.paste(Image.fromarray(cleaned), (glow.left - left, glow.top - top))
    canvas = canvas.resize((layout['size'], layout['size']), Image.Resampling.LANCZOS)
    args.texture.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(args.texture)

    #重新读取实际保存的 PSD，核对像素通道与蒙版，防止仅修改内存预览。
    saved = PSDImage.open(args.output, encoding='utf-8')
    saved_glow = next(layer for layer in saved[1][0] if layer.name == '特效')
    if not np.array_equal(np.array(saved_glow.topil().convert('RGBA')), original):
        raise ValueError('另存 PSD 后原始像素发生变化。')
    if not np.array_equal(np.array(saved_glow.mask.topil()), np.array(mask)):
        raise ValueError('另存 PSD 后星光分离蒙版不一致。')
    print(f'保留 {int(keep.sum())} 个原始星光像素，分离 {removed_count} 个静态散点区域。')
    print(f'PSD：{args.output}')
    print(f'贴图：{args.texture}')


if __name__ == '__main__':
    main()
