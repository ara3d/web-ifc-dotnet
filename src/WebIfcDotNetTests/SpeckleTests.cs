namespace WebIfcDotNetTests;

public static class SpeckleTests
{
    [Test]
    public static void SpeckleWriter()
    {
        // VERY ODD!! Face indices have a "0" before each of them!?

        // extractFaces(indices) {
        //   const faces = []
        //   for (let i = 0; i < indices.length; i++) {
        //     if (i % 3 === 0) faces.push(0)
        //     faces.push(indices[i])
        //   }
        //   return faces
        // }

        // extractVertexData(vertexData, matrix) {
        //   const vertices = []
        //   const normals = []
        //   let isNormalData = false
        //   for (let i = 0; i < vertexData.length; i++) {
        //     isNormalData ? normals.push(vertexData[i]) : vertices.push(vertexData[i])
        //     if ((i + 1) % 3 === 0) isNormalData = !isNormalData
        //   }

        //   // apply the transform
        //   for (let k = 0; k < vertices.length; k += 3) {
        //     const x = vertices[k],
        //       y = vertices[k + 1],
        //       z = vertices[k + 2]
        //     vertices[k] = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12]
        //     vertices[k + 1] =
        //       (matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14]) * -1
        //     vertices[k + 2] = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13]
        //   }

        //   return { vertices, normals }
        // }

        // const speckleMesh = {
        //   // eslint-disable-next-line camelcase
        //   speckle_type: 'Objects.Geometry.Mesh',
        //   units: 'm',
        //   volume: 0,
        //   area: 0,
        //   vertices,
        //   faces,
        //   renderMaterial: placedGeometry.color
        //     ? this.colorToMaterial(placedGeometry.color)
        //     : null
        // }
        //
        // geometryReferences[mesh.expressID].push({
        //   // eslint-disable-next-line camelcase
        //   speckle_type: 'reference',
        //   referencedId: speckleMesh.id
        // })
        //
        //await this.serverApi.saveObjectBatch(speckleMeshes)

        //  async createSpatialStructure() {
        //    const chunks = await this.getSpatialTreeChunks()
        //    const allProjectLines = await this.ifcapi.GetLineIDsWithType(
        //      this.modelId,
        //      WebIFC.IFCPROJECT
        //    )
        //    const project = {
        //      expressID: allProjectLines.get(0),
        //      type: 'IFCPROJECT',
        //      speckle_type: 'Base',
        //      elements: []
        //    }
        //    await this.populateSpatialNode(project, chunks, [], 0)
        //  }


        // Spatial node magic hand wavy stuff.
        //  async populateSpatialNode(node, chunks, closures, depth) {
        //    depth++
        //    this.logger.debug(`${this.spatialNodeCount++} nodes generated.`)
        //    closures.push([])
        //    await this.getChildren(node, chunks, PropNames.aggregates, closures, depth)
        //    await this.getChildren(node, chunks, PropNames.spatial, closures, depth)

        //    node.closure = [...new Set(closures.pop())]

        //    // get geometry, set displayValue
        //    // add geometry ids to closure
        //    if (
        //      this.geometryReferences[node.expressID] &&
        //      this.geometryReferences[node.expressID].length !== 0
        //    ) {
        //      node['@displayValue'] = this.geometryReferences[node.expressID]
        //      node.closure.push(
        //        ...this.geometryReferences[node.expressID].map((ref) => ref.referencedId)
        //      )
        //    }
        //    // node.closureLen = node.closure.length
        //    node.__closure = this.formatClosure(node.closure)
        //    node.id = getHash(node)

        //    // Save to db
        //    this.objectBucket.push(node)
        //    if (this.objectBucket.length > 3000) {
        //      await this.flushObjectBucket()
        //    }

        //    // remove project level node closure
        //    if (depth === 1) {
        //      delete node.closure
        //    }
        //    return node.id
        //  }


        // const PropNames = {
        // aggregates: {
        //   name: WebIFC.IFCRELAGGREGATES,
        //   relating: 'RelatingObject',
        //   related: 'RelatedObjects',
        //   key: 'elements'
        // },
        // spatial: {
        //   name: WebIFC.IFCRELCONTAINEDINSPATIALSTRUCTURE,
        //   relating: 'RelatingStructure',
        //   related: 'RelatedElements',
        //   key: 'elements'
        // },
    }
}