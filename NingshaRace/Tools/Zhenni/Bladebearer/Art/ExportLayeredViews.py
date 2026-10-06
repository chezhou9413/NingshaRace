import argparse
import copy
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


#按已核对的新原稿拆出独立朝向，不从合并图反推火焰或镜像侧面。
LAYERS = {
    'East': ['Cape', 'FlameBody', 'Flame4', 'Head', 'Body', 'Flame2', 'Flame1', 'Flame3', 'Glow'],
    'West': ['Cape', 'FlameBody', 'Flame4', 'Head', 'HeadDetail', 'Body', 'Flame1', 'Flame3', 'Glow'],
    'North': ['Glow', 'FlameBody', 'Flame4', 'Body', 'Cape', 'Head'],
}
LABELS = {'East': '右侧', 'West': '左侧', 'North': '背面'}
SOUTH_ORDER = ['Cape', 'FlameBody', 'Head', 'Body', 'Flame4', 'Flame3', 'Flame2', 'Flame1', 'Glow']
SOUTH_RECT = (971, 382, 2059, 1470)
#头盔中心和顶部对齐原正面，保留原稿的身体、披风和下摆相对位置。
RECTS = {'East': (269.5, 388, 1357.5, 1476), 'West': (2269.5, 385, 3357.5, 1473),
         'North': (1643, 381, 2731, 1469)}
PARTICLE_POINTS = {
    'East': {'HeadSparks': ((846, 798), (944, 567), 'Flame1'),
             'ChestSparks': ((727, 1005), (553, 850), 'Flame3'),
             'CapeSparks': ((544, 1145), (337, 1326), 'Cape'),
             'LowerSparks': ((692, 1412), (548, 1207), 'Flame4')},
    'West': {'HeadSparks': ((2618, 789), (2490, 572), 'Flame1'),
             'ChestSparks': ((2765, 1004), (2950, 868), 'Flame3'),
             'CapeSparks': ((2928, 1139), (3150, 1300), 'Cape'),
             'LowerSparks': ((2790, 1406), (2935, 1200), 'Flame4')},
    'North': {'HeadSparks': ((2100, 812), (1990, 547), 'Cape'),
              'ChestSparks': ((2110, 1176), (1910, 1040), 'FlameBody'),
              'CapeSparks': ((2270, 1200), (2465, 1385), 'Cape'),
              'LowerSparks': ((2121, 1420), (2293, 1199), 'Flame4')},
}


#解析编辑器和 Def 共用的二维坐标。
def pair(text):
    return tuple(float(v.strip()) for v in text.strip('()').split(','))


#向量直接写入中文工程使用的 XML 格式。
def vector(value):
    return '(' + ', '.join(format(v, '.8g') for v in value) + ')'


#同名字段只更新一次，保留现有参数结构。
def put(part, field, value):
    node = part.find(field)
    if node is None:
        node = ET.SubElement(part, field)
    node.text = str(value)


#原稿坐标换算为统一画布 UV，发射点和方向柄与贴图使用同一基准。
def uv(point, rect):
    x, y, right, bottom = rect
    return ((point[0] - x) / (right - x), 1 - (point[1] - y) / (bottom - y))


#从正面图层中的相对位置映射固定根部和尾端，原图反向的火苗同时翻转传播方向。
def mapped_uv(text, original, layer, rect, mirror):
    x, y = pair(text)
    source_x = SOUTH_RECT[0] + x * 1088
    source_y = SOUTH_RECT[1] + (1 - y) * 1088
    relative_x = (source_x - original.left) / original.width
    relative_y = (source_y - original.top) / original.height
    if mirror:
        relative_x = 1 - relative_x
    point = (layer.left + relative_x * layer.width, layer.top + relative_y * layer.height)
    return tuple(max(0, min(1, v)) for v in uv(point, rect))


#只重新采样到游戏统一画布，不改变原 RGB、Alpha 和部件之间的摆放。
def export_texture(layer, rgba, rect, destination, image_type):
    side = int(rect[2] - rect[0])
    canvas = image_type.new('RGBA', (side, side))
    canvas.paste(rgba, (int(layer.left - rect[0]), int(layer.top - rect[1])))
    result = canvas.resize((1024, 1024), image_type.Resampling.LANCZOS)
    if result.getchannel('A').getbbox() is None or result.getchannel('A').getextrema()[0] != 0:
        raise ValueError('贴图为空或缺少透明背景：' + str(destination))
    destination.parent.mkdir(parents=True, exist_ok=True)
    result.save(destination)


#复用正面 Shader、辉光和闪烁参数，只替换本朝向的素材、根部及分层。
def make_part(template, role, facing, layer, south_layer):
    part = copy.deepcopy(template)
    put(part, 'id', role + '_' + facing)
    put(part, 'label', LABELS[facing] + ' · ' + template.findtext('label'))
    put(part, 'texture', 'Zhenni/Bladebearer/Pawn/Bladebearer_' + role + '_' + facing.lower())
    put(part, 'facing', facing)
    put(part, 'position', '(0, 0.15)')
    put(part, 'size', '(1.8, 1.8)')
    if role == 'HeadDetail':
        put(part, 'label', LABELS[facing] + ' · 头盔补绘')
        put(part, 'layer', '44.2')
    if template.findtext('flame') == 'true':
        mirror = facing == 'West' and role in ('Cape', 'FlameBody', 'Flame1', 'Flame2')
        mirror |= facing == 'East' and role in ('Flame4', 'Flame3')
        for field in ('root', 'tip'):
            put(part, field, vector(mapped_uv(template.findtext(field), south_layer, layer, RECTS[facing], mirror)))
        if mirror:
            dx, dy = pair(part.findtext('displacement'))
            put(part, 'displacement', vector((-dx, dy)))
            put(part, 'flowAngle', -float(part.findtext('flowAngle')))
        if facing == 'East' and role == 'Flame1':
            put(part, 'root', vector(uv((742, 810), RECTS[facing])))
            put(part, 'tip', vector(uv((814, 782), RECTS[facing])))
    if facing == 'North':
        #背面星光在头盔后，披风在躯体前；深度轮廓仍由头盔和实体盔甲负责。
        levels = {'Glow': 40, 'FlameBody': 42, 'Flame4': 44, 'Body': 46, 'Cape': 50, 'Head': 54}
        put(part, 'layer', levels[role])
        if role == 'Cape':
            put(part, 'root', vector(uv((2115, 844), RECTS[facing])))
            put(part, 'tip', vector(uv((2315, 1350), RECTS[facing])))
    return part


#每处火星独立控制，波动引用当前朝向实际存在的火焰。
def make_particle(template, role, facing):
    part = copy.deepcopy(template)
    root, tip, flame = PARTICLE_POINTS[facing][role]
    put(part, 'id', role + '_' + facing)
    put(part, 'label', LABELS[facing] + ' · ' + template.findtext('label'))
    put(part, 'facing', facing)
    put(part, 'root', vector(uv(root, RECTS[facing])))
    put(part, 'tip', vector(uv(tip, RECTS[facing])))
    put(part, 'particleFlame', flame + '_' + facing)
    if facing == 'North':
        levels = {'HeadSparks': 53.5, 'ChestSparks': 43, 'CapeSparks': 51, 'LowerSparks': 45}
        put(part, 'layer', levels[role])
    return part


#拆分星光的静态散点并另存清理稿，原始 PSD 保持不变。
def clean_glow(layer, group, np, image_type, pixel_layer):
    from SplitStarlight import split_connected_starlight
    original = np.array(layer.topil().convert('RGBA'))
    keep, count = split_connected_starlight(original, np)
    cleaned, removed = original.copy(), original.copy()
    cleaned[~keep, 3] = 0
    removed[keep, 3] = 0
    layer.create_mask(image_type.fromarray(np.where(keep, 255, 0).astype('uint8')),
                      top=layer.top, left=layer.left)
    separated = pixel_layer.frompil(image_type.fromarray(removed), group,
        name='静态火星（停绘，原像素保留）', top=layer.top, left=layer.left)
    separated.blend_mode, separated.opacity, separated.visible = layer.blend_mode, layer.opacity, False
    group.insert(list(group).index(layer) + 1, separated)
    return image_type.fromarray(cleaned), count


#接入三个分层朝向，现有正面、武器、动画和美术调参保持原值。
def main():
    parser = argparse.ArgumentParser(description='导出震尼拥刀者独立侧面和背面图层。')
    parser.add_argument('--psd', type=Path, required=True)
    parser.add_argument('--mod', type=Path, required=True)
    parser.add_argument('--cleaned-psd', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    parser.add_argument('--dependencies', type=Path, required=True)
    args = parser.parse_args()
    if args.psd.resolve() == args.cleaned_psd.resolve():
        raise ValueError('清理稿必须另存，不能覆盖输入 PSD。')
    sys.path.insert(0, str(args.dependencies))
    import numpy as np
    from PIL import Image
    from psd_tools import PSDImage
    from psd_tools.api.layers import PixelLayer
    psd = PSDImage.open(args.psd)
    if psd.size != (3445, 1596):
        raise ValueError('PSD 尺寸已变化，请核对图层与画布。')
    root = psd[1]
    south = dict(zip(SOUTH_ORDER, list(root[0])[:9]))
    groups = {'East': root[1], 'West': root[2], 'North': root[3][0]}
    appearance_path = args.mod / '1.6/Defs/Zhenni/Bladebearer/BladebearerAppearance.xml'
    document = ET.parse(appearance_path)
    parts = document.getroot()[0].find('parts')
    templates = {p.findtext('id'): p for p in parts if p.findtext('facing') in ('South', 'All')}
    kept = [p for p in parts if p.findtext('facing') in ('South', 'All')]
    records, added = [], []
    for facing, roles in LAYERS.items():
        group = groups[facing]
        layers = list(group)
        if len(layers) != len(roles) or any(l.has_mask() for l in layers):
            raise ValueError(facing + ' 图层数量或蒙版已变化，请核对源稿。')
        for role, layer in zip(roles, layers):
            rgba = layer.topil().convert('RGBA')
            removed = 0
            if role == 'Glow':
                rgba, removed = clean_glow(layer, group, np, Image, PixelLayer)
            template_role = 'Head' if role == 'HeadDetail' else role
            part = make_part(templates[template_role], role, facing, layer, south[template_role])
            rect = RECTS[facing]
            if role == 'Glow' and layer.left < rect[0]:
                #长星芒超出身体画布时平移星光画布，再用部件位置补偿，保留完整尖芒。
                rect = (layer.left, rect[1], layer.left + 1088, rect[3])
                put(part, 'position', vector(((rect[0] - RECTS[facing][0]) * 1.8 / 1088, 0.15)))
            texture = args.mod / ('1.6/Textures/' + part.findtext('texture') + '.png')
            export_texture(layer, rgba, rect, texture, Image)
            added.append(part)
            records.append({'id': part.findtext('id'), 'sourceLayer': layer.name, 'sourceBounds': layer.bbox,
                'sourceRect': rect, 'texture': part.findtext('texture'), 'removedSparkComponents': removed})
        for role in PARTICLE_POINTS[facing]:
            added.append(make_particle(templates[role], role, facing))
    #清理稿同时分离正面散点；游戏仍保留已调好的正面贴图和参数。
    clean_glow(south['Glow'], root[0], np, Image, PixelLayer)
    for layer in root[0]:
        if layer.name in ('图层 2', '鬼火'):
            layer.visible = False
    args.cleaned_psd.parent.mkdir(parents=True, exist_ok=True)
    psd.save(args.cleaned_psd, encoding='utf-8')
    parts[:] = kept + added
    ET.indent(document, space='  ')
    document.write(appearance_path, encoding='utf-8', xml_declaration=True)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps({'sourcePSD': str(args.psd), 'cleanedPSD': str(args.cleaned_psd),
        'southSourceRect': SOUTH_RECT, 'newTextureCount': len(records), 'addedPartCount': len(added),
        'layers': records}, ensure_ascii=False, indent=2), encoding='utf-8')
    print('导出贴图：', len(records), '；新增独立部件：', len(added), '；外观配置：', appearance_path)


if __name__ == '__main__':
    main()
