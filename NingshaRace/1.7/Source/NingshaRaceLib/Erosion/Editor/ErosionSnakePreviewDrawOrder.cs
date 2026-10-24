using System.Collections.Generic;
using HarmonyLib;
using NingshaRaceLib.Erosion.Rendering;
using NingshaRaceLib.Erosion.Utility;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Editor
{
    //即时预览不会经过 Unity 的材质队列，需先画实体，再画透明头部黑雾。
    internal sealed class ErosionSnakePreviewDrawOrder
    {
        //保存实际相机深度和原始序号，保证相同深度的部件顺序稳定。
        private struct Entry
        {
            internal PawnGraphicDrawRequest Request;
            internal int Queue, Order;
            internal float Depth;
        }

        private readonly List<PawnGraphicDrawRequest> original = new List<PawnGraphicDrawRequest>();
        private readonly List<Entry> sorted = new List<Entry>();

        //只重排当前预览的绘制请求，沿用原版材质、矩阵与绘制回调。
        internal void Begin(List<PawnGraphicDrawRequest> requests, Camera camera)
        {
            original.Clear(); original.AddRange(requests); sorted.Clear();
            Vector3 cameraPosition = camera.transform.position, forward = camera.transform.forward;
            for (int i = 0; i < requests.Count; i++)
            {
                PawnGraphicDrawRequest request = requests[i];
                sorted.Add(new Entry
                {
                    Request = request, Order = i, Queue = request.material != null ? request.material.renderQueue : 2000,
                    Depth = Vector3.Dot((Vector3)request.preDrawnComputedMatrix.GetColumn(3) - cameraPosition, forward)
                });
            }
            sorted.Sort(Compare);
            for (int i = 0; i < sorted.Count; i++) requests[i] = sorted[i].Request;
        }

        //透明材质按远到近叠加，实体仍由深度测试决定遮挡。
        private static int Compare(Entry left, Entry right)
        {
            int order = left.Queue.CompareTo(right.Queue);
            if (order == 0 && left.Queue > 2500) order = right.Depth.CompareTo(left.Depth);
            return order != 0 ? order : left.Order.CompareTo(right.Order);
        }

        //即使预览抛出异常，也恢复渲染树缓存，避免影响后续地图绘制。
        internal void End(List<PawnGraphicDrawRequest> requests)
        {
            requests.Clear(); requests.AddRange(original);
            original.Clear(); sorted.Clear();
        }
    }

    //在主线程准备动态头部材质，并让头像与蛇头预览共用即时绘制排序。
    [HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.Draw))]
    internal static class Patch_ErosionHeadDrawPreparation
    {
        private static readonly Stack<ErosionSnakePreviewDrawOrder> DrawOrders = new Stack<ErosionSnakePreviewDrawOrder>();

        //动画换图产生的材质在绘制前补齐缓存，后台线程无需创建 Unity 对象。
        [HarmonyPrefix]
        private static void Prefix(PawnRenderTree __instance, PawnDrawParms parms,
            List<PawnGraphicDrawRequest> ___drawRequests, out ErosionSnakePreviewDrawOrder __state)
        {
            __state = null;
            if (!ErosionPawnUtility.IsErosionBody(__instance.pawn)) return;
            for (int i = 0; i < ___drawRequests.Count; i++)
            {
                PawnGraphicDrawRequest request = ___drawRequests[i];
                if (ReferenceEquals(request.material, null)
                    || !ErosionBodyRenderingUtility.IsErosionHeadNode(request.node, __instance.pawn)) continue;
                request.material = ErosionBodyHeadMaterialPool.GetOrCreateMaterial(request.material, __instance.pawn.Dead);
                ___drawRequests[i] = request;
            }
            if (!parms.DrawNow) return;
            __state = DrawOrders.Count > 0 ? DrawOrders.Pop() : new ErosionSnakePreviewDrawOrder();
            __state.Begin(___drawRequests, Find.PawnCacheCamera);
        }

        //保留原异常的传播，只清理本次预览排序。
        [HarmonyFinalizer]
        private static void Finalizer(List<PawnGraphicDrawRequest> ___drawRequests, ErosionSnakePreviewDrawOrder __state)
        {
            if (__state == null) return;
            __state.End(___drawRequests);
            DrawOrders.Push(__state);
        }
    }
}
