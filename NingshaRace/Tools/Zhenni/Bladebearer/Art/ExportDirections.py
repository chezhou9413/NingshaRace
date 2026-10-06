import argparse
import copy
import math
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

import numpy as np
from PIL import Image


#裁切合并原稿的三个方向，按头顶、主体底部和胸甲中心对齐既有正面画布。
VIEWS = {
    'West': ((65, 160, 1080, 1320), 324, 1288, 601, '左侧合成图'),
    'East': ((2045, 160, 3110, 1320), 333, 1302, 2500, '右侧合成图'),
    'North': ((3110, 160, 3910, 1320), 333, 1248, 3409, '背面合成图'),
}


#保留图中像素、透明度与静态特效，仅裁切和统一画布，不重新绘画分层。
def export_views(source, texture_root):
    from psd_tools import PSDImage
    psd = PSDImage.open(source)
    if len(psd) != 1 or psd[0].is_group() or psd.size != (4027, 1484):
        raise ValueError('原稿结构或尺寸已变化，请重新核对裁切坐标。')
    original = psd.topil().convert('RGBA')
    for facing, (box, top, bottom, center, _) in VIEWS.items():
        scale = 857.0 / (bottom - top)
        cropped = original.crop(box)
        #目标头顶 y=144、主体底部 y=1001、中心 x=448，与正面盔甲和火焰躯体对齐。
        origin_x = center - 448 / scale - box[0]
        origin_y = top - 144 / scale - box[1]
        image = cropped.transform((1024, 1024), Image.Transform.AFFINE,
            (1 / scale, 0, origin_x, 0, 1 / scale, origin_y), Image.Resampling.BICUBIC)
        output = texture_root / 'Zhenni/Bladebearer/Pawn' / ('Bladebearer_View_' + facing.lower() + '.png')
        output.parent.mkdir(parents=True, exist_ok=True)
        image.save(output)
        print(f'{facing}: {output}')


#增加朝向及绑定信息；刀上部件以原版武器中心为局部原点。
def configure_appearance(path):
    document = ET.parse(path)
    appearance = document.getroot()[0]
    appearance.find('label').text = '震尼拥刀者默认外观'
    parts = appearance.find('parts')
    weapon_layers = {'Dagger': '0', 'DaggerFlame': '2', 'DaggerSparks': '4'}
    for part in list(parts):
        if part.findtext('id', '').startswith('View_') or part.findtext('id') == 'GhostFire':
            parts.remove(part)
            continue
        weapon = part.findtext('id') in weapon_layers
        put(part, 'facing', 'All' if weapon else 'South')
        put(part, 'attachment', 'Weapon' if weapon else 'Body')
        if weapon:
            put(part, 'position', '(0, 0)')
            put(part, 'layer', weapon_layers[part.findtext('id')])
    template = next(part for part in parts if part.findtext('id') == 'Body')
    for facing, (_, _, _, _, label) in VIEWS.items():
        part = copy.deepcopy(template)
        values = {'id': 'View_' + facing, 'label': label, 'texture': 'Zhenni/Bladebearer/Pawn/Bladebearer_View_' + facing.lower(),
            'facing': facing, 'attachment': 'Body', 'layer': '50', 'position': '(0, 0.15)', 'size': '(1.8, 1.8)',
            'displacement': '(0, 0)', 'breakup': '0', 'bloomIntensity': '0'}
        for key, value in values.items():
            put(part, key, value)
        parts.append(part)
    ET.indent(document, space='  ')
    document.write(path, encoding='utf-8', xml_declaration=True)


#写入实际需要的字段，重复导出时更新同一字段。
def put(part, key, value):
    node = part.find(key)
    if node is None:
        node = ET.SubElement(part, key)
    node.text = value


#读取美工 XML 使用的二维向量。
def pair(text):
    return tuple(float(value.strip()) for value in text.strip('()').split(','))


#静态图鉴按渲染层级合成现有贴图，武器采用游戏同一闲置握持姿态，不模拟动态粒子。
def render_icon(texture_root, appearance_path, facing='South', output=None,
                melee_path=None, weapon_path=None, weapon_part='Dagger'):
    parts = ET.parse(appearance_path).getroot()[0].find('parts')
    #沿用原版闲置持械角，再叠加四向局部校准；镜像和刀柄握点与运行时一致。
    melee = ET.parse(melee_path or appearance_path.with_name('BladebearerMeleeAnimation.xml')).getroot()[0]
    weapon = ET.parse(weapon_path or appearance_path.with_name('BladebearerWeapon.xml')).getroot()[0]
    equipped_angle = float(weapon.findtext('equippedAngleOffset'))
    weapon_angle = -53 - equipped_angle if facing == 'West' else 53 + equipped_angle
    mirror = (facing == 'West') != (melee.findtext('mirrorBlade') == 'true')
    correction = float(melee.findtext('holdAngle' + facing))
    weapon_angle += -correction if mirror else correction
    hand = np.array(pair(melee.findtext('hand' + facing)))
    grip = np.array(pair(melee.findtext('grip'))) - 0.5
    blade = next(part for part in parts if part.findtext('id') == weapon_part)
    blade_angle = math.radians(float(blade.findtext('angle')))
    blade_rotation = np.array([[math.cos(blade_angle), math.sin(blade_angle)], [-math.sin(blade_angle), math.cos(blade_angle)]])
    grip = blade_rotation @ (grip * np.array(pair(blade.findtext('size')))) + np.array(pair(blade.findtext('position')))
    if mirror:
        grip[0] *= -1
    radians = math.radians(weapon_angle)
    weapon_rotation = np.array([[math.cos(radians), math.sin(radians)], [-math.sin(radians), math.cos(radians)]])
    weapon_origin = hand - weapon_rotation @ grip
    canvas = np.zeros((1024, 1024, 4), np.float32)
    #保持完整盔甲和刀刃，留出图鉴边距；纵坐标与 Pawn 平面 Z 相反。
    pixels_per_unit, origin = 520.0, (550.0, 536.0)
    weapon_layer = -10 if facing == 'North' else 90
    ordered = sorted(parts, key=lambda p: float(p.findtext('layer')) + (weapon_layer if p.findtext('attachment') == 'Weapon' else 0))
    for part in ordered:
        if part.findtext('enabled') != 'true' or part.findtext('particle') == 'true':
            continue
        if part.findtext('facing') not in (facing, 'All') or part.findtext('id') == 'GhostFire':
            continue
        source = Image.open(texture_root / (part.findtext('texture') + '.png')).convert('RGBA')
        px, pz = pair(part.findtext('position'))
        sx, sz = pair(part.findtext('size'))
        angle = float(part.findtext('angle'))
        if part.findtext('attachment') == 'Weapon':
            #与游戏一致：沿竖直刀身翻转刃口，附着特效共用同一镜像。
            if mirror:
                source = source.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
                px, angle = -px, -angle
            radians = math.radians(weapon_angle)
            px, pz = weapon_rotation @ np.array([px, pz]) + weapon_origin
            angle += weapon_angle
        radians = math.radians(angle)
        cosine, sine = math.cos(radians), math.sin(radians)
        #将源画布的像素坐标转换到最终图鉴，再反求 Pillow 所需的采样矩阵。
        transform = np.array([[cosine * sx * pixels_per_unit / source.width, -sine * sz * pixels_per_unit / source.height, 0],
                              [sine * sx * pixels_per_unit / source.width, cosine * sz * pixels_per_unit / source.height, 0], [0, 0, 1]], float)
        transform[:2, 2] = np.array([origin[0] + px * pixels_per_unit, origin[1] - pz * pixels_per_unit]) - transform[:2, :2] @ np.array(source.size) / 2
        inverse = np.linalg.inv(transform)
        layer = source.transform((1024, 1024), Image.Transform.AFFINE, tuple(inverse[:2].flat), Image.Resampling.BICUBIC)
        rgba = np.asarray(layer, dtype=np.float32) / 255
        tint = np.array(pair(part.findtext('color')))
        rgba *= tint
        alpha = rgba[:, :, 3:4]
        additive = float(part.findtext('additive'))
        intensity = float(part.findtext('lightIntensity', '1')) if additive > 0 else 1
        canvas[:, :, :3] = rgba[:, :, :3] * alpha * intensity + canvas[:, :, :3] * (1 - alpha * (1 - additive))
        canvas[:, :, 3:4] = alpha + canvas[:, :, 3:4] * (1 - alpha)
    #转换预乘颜色为 PNG 直通 Alpha，避免图鉴边缘出现黑圈。
    canvas[:, :, :3] /= np.maximum(canvas[:, :, 3:4], 1e-6)
    if output is None:
        output = texture_root / 'UI/CodexEntries/NingshaRace/Zhenni/Bladebearer/Bladebearer.png'
    output.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.uint8(np.clip(canvas, 0, 1) * 255)).save(output)
    print('图鉴彩图：', output)


#显式指定原稿与 Mod 路径，不覆盖 PSD；只维护本单位的导出资源和默认外观。
def main():
    parser = argparse.ArgumentParser(description='导出震尼拥刀者四向素材与持刀图鉴。')
    parser.add_argument('--psd', type=Path, required=True)
    parser.add_argument('--mod', type=Path, required=True)
    parser.add_argument('--dependencies', type=Path, required=True)
    args = parser.parse_args()
    sys.path.insert(0, str(args.dependencies))
    textures = args.mod / '1.6/Textures'
    appearance = args.mod / '1.6/Defs/Zhenni/Bladebearer/BladebearerAppearance.xml'
    export_views(args.psd, textures)
    configure_appearance(appearance)
    render_icon(textures, appearance)


if __name__ == '__main__':
    main()
