// Three.js viewer for NURBS visualization
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

let scene, camera, renderer, controls;
let pointsObject;
let meshObject;

export function initThreeJS(containerId) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Container not found:', containerId);
        return false;
    }

    // Scene setup
    scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf0f0f0);

    // Camera setup
    camera = new THREE.PerspectiveCamera(
        75,
        container.clientWidth / container.clientHeight,
        0.1,
        1000
    );
    // Set Z-axis as up direction (must be set before positioning camera)
    camera.up.set(0, 0, 1);
    camera.position.set(3, 8, 3);
    camera.lookAt(2, 2, 1);

    // Renderer setup
    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(container.clientWidth, container.clientHeight);
    renderer.setPixelRatio(window.devicePixelRatio);
    container.appendChild(renderer.domElement);

    // Controls
    controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.05;

    // Add axes helper
    const axesHelper = new THREE.AxesHelper(5);
    scene.add(axesHelper);

    // Add grid helper (rotated for Z-up orientation)
    const gridHelper = new THREE.GridHelper(10, 10);
    gridHelper.rotation.x = Math.PI / 2; // Rotate grid to XY plane
    gridHelper.position.z = -0.001; // Slightly lower to avoid z-fighting with axes
    scene.add(gridHelper);

    // Add lights
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
    scene.add(ambientLight);

    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.4);
    directionalLight.position.set(5, 10, 7.5);
    scene.add(directionalLight);

    // Handle window resize
    window.addEventListener('resize', onWindowResize);

    // Start animation loop
    animate();

    return true;
}

function onWindowResize() {
    const container = renderer.domElement.parentElement;
    if (!container) return;

    camera.aspect = container.clientWidth / container.clientHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(container.clientWidth, container.clientHeight);
}

function animate() {
    requestAnimationFrame(animate);
    controls.update();
    renderer.render(scene, camera);
}

export function displayPoints(pointsData) {
    console.log('displayPoints called with:', pointsData);
    
    // Remove existing points if any
    if (pointsObject) {
        scene.remove(pointsObject);
        pointsObject.geometry.dispose();
        pointsObject.material.dispose();
    }

    // Create geometry from points data
    const geometry = new THREE.BufferGeometry();
    const positions = new Float32Array(pointsData.length * 3);

    for (let i = 0; i < pointsData.length; i++) {
        console.log(`Point ${i}:`, pointsData[i]);
        positions[i * 3] = pointsData[i].x;
        positions[i * 3 + 1] = pointsData[i].y;
        positions[i * 3 + 2] = pointsData[i].z;
    }

    console.log('Positions array:', positions);
    
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));

    // Create material for points
    const material = new THREE.PointsMaterial({
        color: 0xff0000,
        size: 10,
        sizeAttenuation: false
    });

    // Create points object
    pointsObject = new THREE.Points(geometry, material);
    scene.add(pointsObject);

    console.log('Displayed', pointsData.length, 'points');
}

export function displayMesh(meshData) {
    console.log('displayMesh called with:', meshData);
    
    // Remove existing mesh if any
    if (meshObject) {
        scene.remove(meshObject);
        meshObject.geometry.dispose();
        meshObject.material.dispose();
    }

    // Create geometry from mesh data
    const geometry = new THREE.BufferGeometry();
    
    // Set vertices
    const vertices = new Float32Array(meshData.vertices.length * 3);
    for (let i = 0; i < meshData.vertices.length; i++) {
        vertices[i * 3] = meshData.vertices[i].x;
        vertices[i * 3 + 1] = meshData.vertices[i].y;
        vertices[i * 3 + 2] = meshData.vertices[i].z;
    }
    geometry.setAttribute('position', new THREE.BufferAttribute(vertices, 3));
    
    // Set faces (indices)
    const indices = new Uint32Array(meshData.faces.length * 3);
    for (let i = 0; i < meshData.faces.length; i++) {
        indices[i * 3] = meshData.faces[i].a;
        indices[i * 3 + 1] = meshData.faces[i].b;
        indices[i * 3 + 2] = meshData.faces[i].c;
    }
    geometry.setIndex(new THREE.BufferAttribute(indices, 1));
    
    // Compute normals for proper lighting
    geometry.computeVertexNormals();

    // Create material with both solid and wireframe
    const material = new THREE.MeshPhongMaterial({
        color: 0x00aaff,
        shininess: 30,
        side: THREE.DoubleSide,
        flatShading: false,
        polygonOffset: true,
        polygonOffsetFactor: 1,
        polygonOffsetUnits: 1
    });

    // Create mesh object
    meshObject = new THREE.Mesh(geometry, material);
    scene.add(meshObject);
    
    // Create wireframe showing only quad edges (not triangle diagonals)
    // We'll manually create edges by analyzing the mesh structure
    const edges = [];
    const edgeMap = new Map();
    
    // Helper to create edge key (sorted to avoid duplicates)
    const getEdgeKey = (a, b) => a < b ? `${a}-${b}` : `${b}-${a}`;
    
    // Collect all triangle edges and count occurrences
    for (let i = 0; i < meshData.faces.length; i++) {
        const face = meshData.faces[i];
        const edges_in_face = [
            [face.a, face.b],
            [face.b, face.c],
            [face.c, face.a]
        ];
        
        for (const [v1, v2] of edges_in_face) {
            const key = getEdgeKey(v1, v2);
            edgeMap.set(key, (edgeMap.get(key) || 0) + 1);
        }
    }
    
    // Only draw edges that are shared by exactly 2 triangles (quad boundaries)
    // or are on the boundary (appear only once)
    const edgePositions = [];
    for (const [key, count] of edgeMap.entries()) {
        const [v1, v2] = key.split('-').map(Number);
        edgePositions.push(
            meshData.vertices[v1].x, meshData.vertices[v1].y, meshData.vertices[v1].z,
            meshData.vertices[v2].x, meshData.vertices[v2].y, meshData.vertices[v2].z
        );
    }
    
    const edgeGeometry = new THREE.BufferGeometry();
    edgeGeometry.setAttribute('position', new THREE.Float32BufferAttribute(edgePositions, 3));
    const edgeMaterial = new THREE.LineBasicMaterial({ 
        color: 0x000000, 
        linewidth: 1,
        depthTest: true
    });
    const edgeLines = new THREE.LineSegments(edgeGeometry, edgeMaterial);
    meshObject.add(edgeLines);

    console.log('Displayed mesh with', meshData.vertices.length, 'vertices and', meshData.faces.length, 'faces');
}

export function clearScene() {
    if (pointsObject) {
        scene.remove(pointsObject);
        pointsObject.geometry.dispose();
        pointsObject.material.dispose();
        pointsObject = null;
    }
    if (meshObject) {
        scene.remove(meshObject);
        meshObject.geometry.dispose();
        meshObject.material.dispose();
        meshObject = null;
    }
}

export function dispose() {
    window.removeEventListener('resize', onWindowResize);
    
    if (controls) {
        controls.dispose();
    }
    
    if (renderer) {
        renderer.dispose();
        if (renderer.domElement && renderer.domElement.parentElement) {
            renderer.domElement.parentElement.removeChild(renderer.domElement);
        }
    }
    
    clearScene();
}
