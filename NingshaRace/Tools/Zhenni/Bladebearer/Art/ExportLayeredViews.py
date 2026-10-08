import argparse
import copy
import json
import math
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


#按原稿从下到上的图层顺序导出，四向继续使用既有身体画布。
LAYERS = {
    'South': ['Cape', 'FlameBody', 'Head', 'Body', 'Flame4', 'Flame3', 'Flame2', 'Flame1', 'Glow'],
    'East': ['Cape', 'FlameBody', 'Flame4', 'Head', 'Body', 'Flame2', 'Flame1', 'Flame3', 'Glow'],
    'West': ['Cape', 'FlameBody', 'Flame4', 'Head', 'HeadDetail', 'Body', 'Flame1', 'Flame3', 'Glow'],
    'North': ['Glow', 'Flame2', 'Flame1', 'Flame3', 'FlameBody', 'Flame4', 'Body', 'Cape', 'Head'],
}
RECTS = {'South': (971, 382, 2059, 1470), 'East': (269.5, 388, 1357.5, 1476),
         'West': (2269.5, 385, 3357.5, 1473), 'North': (1643, 381, 2731, 1469)}
WEAPON_RECT = (832, 636, 1376, 1180)
BACK_FLAMES = {
    'Flame2': (40.2, (2208, 878), (2330, 812), False),
    'Flame1': (40.4, (2110, 806), (1886, 779), True),
    'Flame3': (40.6, (2100, 1035), (1907, 928), True),
}


#解析美术参数使用的二维坐标。
def pair(text):
    return tuple(float(v.strip()) for v in text.strip('()').split(','))


#以可直接导入游戏的格式保存向量。
def vector(value):
    return '(' + ', '.join(format(v, '.8g') for v in value) + ')'


#同名字段原位更新，不重建其他美术参数。
def put(part, field, value):
    node = part.find(field)
    if node is None:
        node = ET.SubElement(part, field)
    node.text = str(value)


#原稿坐标与统一画布 UV 使用同一基准。
def uv(point, rect):
    return ((point[0] - rect[0]) / (rect[2] - rect[0]),
            1 - (point[1] - rect[1]) / (rect[3] - rect[1]))


#只补充尚未配置的新图层，重复导出保留已经编辑过的参数。
def add_part(parts, template, values):
    part = copy.deepcopy(template)
    for key, value in values.items():
        put(part, key, value)
    parts.append(part)
    return part


#三团背面火焰位于披风和头盔后，方向取自各自原画的根部与尖端。
def configure_back(parts):
    lookup = {p.findtext('id'): p for p in parts}
    created = []
    for role, (level, root, tip, mirror) in BACK_FLAMES.items():
        key = role + '_North'
        if key in lookup:
            continue
        template = lookup[role]
        part = add_part(parts, template, {
            'id': key, 'label': '背面 · ' + template.findtext('label'),
            'texture': 'Zhenni/Bladebearer/Pawn/Bladebearer_' + role + '_north',
            'facing': 'North', 'attachment': 'Body', 'enabled': 'true',
            'position': '(0, 0.15)', 'layer': level,
            'root': vector(uv(root, RECTS['North'])), 'tip': vector(uv(tip, RECTS['North']))})
        if mirror:
            dx, dy = pair(part.findtext('displacement'))
            put(part, 'displacement', vector((-dx, dy)))
            put(part, 'flowAngle', -float(part.findtext('flowAngle')))
        created.append(role)
    #已有头部和胸部发射器改跟随对应火焰，新增肩部发射器补齐第三团。
    particle_points = (
        ('HeadSparks_North', 'Flame1', (1995, 792), (1815, 735), 40.5),
        ('ChestSparks_North', 'Flame3', (2020, 998), (1880, 850), 40.7),
        ('ShoulderSparks_North', 'Flame2', (2250, 858), (2370, 780), 40.3))
    for key, role, root, tip, level in particle_points:
        if key == 'ShoulderSparks_North' and key not in lookup:
            lookup[key] = add_part(parts, lookup['HeadSparks_North'], {
                'id': key, 'label': '背面 · 肩部动态火星', 'particleRate': '8'})
            initialize = True
        else:
            initialize = role in created
        if initialize:
            for field, value in {
                'particleFlame': role + '_North',
                'texture': 'Zhenni/Bladebearer/Pawn/Bladebearer_' + role + '_north',
                'root': vector(uv(root, RECTS['North'])),
                'tip': vector(uv(tip, RECTS['North'])), 'flowAngle': '0', 'layer': level}.items():
                put(lookup[key], field, value)


#刀上两团火焰共用原握持画布，各自拥有置换根部、相位和火星。
def configure_dagger(parts):
    lookup = {p.findtext('id'): p for p in parts}
    if 'DaggerTipFlame' in lookup:
        return
    lower = lookup['DaggerFlame']
    add_part(parts, lower, {
        'id': 'DaggerTipFlame', 'label': '匕首上段火焰',
        'texture': 'Zhenni/Bladebearer/Weapon/Bladebearer_DaggerTipFlame',
        'layer': '3', 'root': vector(uv((1063, 750), WEAPON_RECT)),
        'tip': vector(uv((1125, 817), WEAPON_RECT)),
        'displacement': '(0.036, 0.014)', 'rootLock': '0.16', 'breakup': '0.7',
        'phase': format(float(lower.findtext('phase')) + 1.23, '.8g')})
    put(lower, 'label', '匕首下段火焰')
    put(lower, 'root', vector(uv((1073, 865), WEAPON_RECT)))
    put(lower, 'tip', vector(uv((1200, 1055), WEAPON_RECT)))
    add_part(parts, lookup['DaggerSparks'], {
        'id': 'DaggerTipSparks', 'label': '匕首上段动态火星',
        'texture': 'Zhenni/Bladebearer/Weapon/Bladebearer_DaggerTipFlame',
        'particleFlame': 'DaggerTipFlame', 'layer': '4.2',
        'root': vector(uv((1080, 771), WEAPON_RECT)),
        'tip': vector(uv((1150, 842), WEAPON_RECT)),
        'particleRate': '6', 'particleSize': '0.010', 'particleSpeed': '0.17',
        'particleSpread': '30', 'particleSway': '0.035'})


#单层按原 RGB、Alpha 贴入固定画布，武器画布不变以保留真实握点。
def export_texture(layer, rect, size, destination, image_type):
    side = int(rect[2] - rect[0])
    canvas = image_type.new('RGBA', (side, side))
    rgba = layer.topil().convert('RGBA')
    canvas.paste(rgba, (int(layer.left - rect[0]), int(layer.top - rect[1])))
    result = canvas.resize((size, size), image_type.Resampling.LANCZOS)
    if result.getchannel('A').getbbox() is None or result.getchannel('A').getextrema()[0] != 0:
        raise ValueError('贴图为空或缺少透明背景：' + str(destination))
    destination.parent.mkdir(parents=True, exist_ok=True)
    result.save(destination)


#星芒超出身体画布时平移其画布，补偿部件位置以保留原稿里的发光中心。
def reposition_glow(part, previous_rect, rect):
    sx, sy = pair(part.findtext('size'))
    dx = (rect[0] - previous_rect[0]) * sx / (rect[2] - rect[0])
    dy = -(rect[1] - previous_rect[1]) * sy / (rect[3] - rect[1])
    angle = math.radians(float(part.findtext('angle')))
    x, y = pair(part.findtext('position'))
    if dx or dy:
        put(part, 'position', vector((x + math.cos(angle) * dx + math.sin(angle) * dy,
                                     y - math.sin(angle) * dx + math.cos(angle) * dy)))


#核对实际分组和层数，拒绝把顺序不同的原稿错配成头部或火焰。
def source_groups(psd):
    if psd.size != (3445, 1596):
        raise ValueError('PSD 尺寸已变化，请核对画布。')
    root = psd[1]
    if len(root) != 7 or root[5].name != '匕首' or root[4].name != '斧头':
        raise ValueError('需要包含独立匕首分组和背面三团火焰的新版 PSD。')
    groups = {'South': root[0], 'East': root[1], 'West': root[2], 'North': root[3][0]}
    names = {'South': '正面', 'East': '右', 'West': '左 副本', 'North': '背面 副本 2'}
    for facing, group in groups.items():
        if group.name != names[facing] or len(group) != len(LAYERS[facing]):
            raise ValueError(facing + ' 图层结构已变化，请核对源稿。')
        for role, layer in zip(LAYERS[facing], group):
            if layer.is_group() or layer.has_mask():
                raise ValueError(facing + '/' + role + ' 不是无蒙版的独立像素层。')
            prefix = {'Cape': '斗篷', 'FlameBody': '火焰躯体', 'Glow': '特效'}.get(role)
            if role.startswith('Flame') and role != 'FlameBody':
                prefix = '火苗' + role[-1]
            if prefix and not layer.name.startswith(prefix):
                raise ValueError(facing + '/' + role + ' 与源图层名称不一致。')
    if len(root[5]) != 4 or len(root[4]) != 2:
        raise ValueError('匕首应有刀身、两团火焰和静态火星；斧头应有实体与火焰两层。')
    return groups


#更新四向身体、独立武器与两种实体外观，复用已有动画和 Shader。
def main():
    parser = argparse.ArgumentParser(description='导出震尼四向图层、双层匕首火焰和背面火焰。')
    parser.add_argument('--psd', type=Path, required=True)
    parser.add_argument('--mod', type=Path, required=True)
    parser.add_argument('--cleaned-psd', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    parser.add_argument('--dependencies', type=Path, required=True)
    args = parser.parse_args()
    if args.psd.resolve() == args.cleaned_psd.resolve():
        raise ValueError('清理稿必须另存，不能覆盖输入 PSD。')
    sys.path.insert(0, str(args.dependencies))
    from PIL import Image
    from psd_tools import PSDImage

    psd = PSDImage.open(args.psd)
    groups = source_groups(psd)
    previous = json.loads(args.report.read_text(encoding='utf-8'))
    previous_rects = {item['id']: item['sourceRect'] for item in previous['layers']}
    documents = {}
    for kind in ('Bladebearer', 'Axebearer'):
        path = args.mod / ('1.6/Defs/Zhenni/' + kind + '/' + kind + 'Appearance.xml')
        document = ET.parse(path)
        parts = document.getroot()[0].find('parts')
        configure_back(parts)
        if kind == 'Bladebearer':
            configure_dagger(parts)
        documents[kind] = (path, document, {p.findtext('id'): p for p in parts})

    records = []
    for facing, roles in LAYERS.items():
        for role, layer in zip(roles, groups[facing]):
            key = role if facing == 'South' else role + '_' + facing
            rect = RECTS[facing]
            if role == 'Glow':
                left = min(rect[0], layer.left)
                rect = (left, rect[1], left + 1088, rect[3])
                for _, _, parts in documents.values():
                    reposition_glow(parts[key], previous_rects.get(key, RECTS[facing]), rect)
            texture = documents['Bladebearer'][2][key].findtext('texture')
            export_texture(layer, rect, 1024, args.mod / ('1.6/Textures/' + texture + '.png'), Image)
            records.append({'id': key, 'sourceLayer': layer.name, 'sourceBounds': layer.bbox,
                            'sourceRect': rect, 'texture': texture})

    weapon_layers = (('Dagger', psd[1][5][0]), ('DaggerFlame', psd[1][5][1]),
                     ('DaggerTipFlame', psd[1][5][2]))
    for key, layer in weapon_layers:
        texture = documents['Bladebearer'][2][key].findtext('texture')
        export_texture(layer, WEAPON_RECT, 512, args.mod / ('1.6/Textures/' + texture + '.png'), Image)
        records.append({'id': key, 'sourceLayer': layer.name, 'sourceBounds': layer.bbox,
                        'sourceRect': WEAPON_RECT, 'texture': texture})

    #斧头保持自己的原画布和参数，同时将来源记录更新到当前 PSD。
    from ExportAxe import export_weapon
    export_weapon(args.psd, args.mod)
    for path, document, _ in documents.values():
        ET.indent(document, space='  ')
        document.write(path, encoding='utf-8', xml_declaration=True)
    #独立鬼火和旧火星底图沿用停绘要求，原始输入稿不写入。
    psd[1][6].visible = False
    psd[1][5][3].visible = False
    args.cleaned_psd.parent.mkdir(parents=True, exist_ok=True)
    psd.save(args.cleaned_psd, encoding='utf-8')
    args.report.write_text(json.dumps({
        'sourcePSD': str(args.psd), 'cleanedPSD': str(args.cleaned_psd),
        'bodyTextureCount': 36, 'daggerTextureCount': 3, 'axeTextureCount': 2,
        'layers': records, 'inactiveLayers': ['鬼火', '匕首/图层 2（静态火星）']
    }, ensure_ascii=False, indent=2), encoding='utf-8')
    print('已导出四向身体 36 层、匕首 3 层和斧头 2 层，更新两种实体外观。')


if __name__ == '__main__':
    sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'Axebearer/Art'))
    main()
