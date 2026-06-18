import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import test from "node:test";

import { optimizeGlb, parseGlb, validateGlb } from "./glb-texture-optimizer.mjs";

const JSON_CHUNK = 0x4e4f534a;
const BIN_CHUNK = 0x004e4942;

function pad(bytes, fill = 0) {
  const result = Buffer.alloc((bytes.length + 3) & ~3, fill);
  bytes.copy(result);
  return result;
}

function createGlb(document, binary) {
  const json = pad(Buffer.from(JSON.stringify(document)), 0x20);
  const bin = pad(binary);
  const output = Buffer.alloc(12 + 8 + json.length + 8 + bin.length);
  output.writeUInt32LE(0x46546c67, 0);
  output.writeUInt32LE(2, 4);
  output.writeUInt32LE(output.length, 8);
  output.writeUInt32LE(json.length, 12);
  output.writeUInt32LE(JSON_CHUNK, 16);
  json.copy(output, 20);
  const binHeader = 20 + json.length;
  output.writeUInt32LE(bin.length, binHeader);
  output.writeUInt32LE(BIN_CHUNK, binHeader + 4);
  bin.copy(output, binHeader + 8);
  return output;
}

test("resizes an embedded image and preserves non-image buffer data", async () => {
  const directory = mkdtempSync(path.join(tmpdir(), "glb-opt-test-"));
  try {
    const pngPath = path.join(directory, "large.png");
    const inputPath = path.join(directory, "input.glb");
    const outputPath = path.join(directory, "output.glb");
    execFileSync("magick", ["-size", "4096x1024", "xc:#336699", pngPath]);
    const png = readFileSync(pngPath);
    const geometry = Buffer.from([1, 3, 3, 7, 9, 11, 13]);
    const imageOffset = (geometry.length + 3) & ~3;
    const binary = Buffer.alloc(imageOffset + png.length);
    geometry.copy(binary);
    png.copy(binary, imageOffset);
    const document = {
      asset: { version: "2.0" },
      buffers: [{ byteLength: binary.length }],
      bufferViews: [
        { buffer: 0, byteOffset: 0, byteLength: geometry.length },
        { buffer: 0, byteOffset: imageOffset, byteLength: png.length },
      ],
      images: [{ bufferView: 1, mimeType: "image/png" }],
      textures: [{ source: 0 }],
    };
    writeFileSync(inputPath, createGlb(document, binary));

    await optimizeGlb(inputPath, outputPath, { maxDimension: 2048 });

    const optimizedBytes = readFileSync(outputPath);
    const parsed = parseGlb(optimizedBytes);
    const geometryView = parsed.document.bufferViews[0];
    assert.deepEqual(
      parsed.binary.subarray(geometryView.byteOffset, geometryView.byteOffset + geometryView.byteLength),
      geometry,
    );
    assert.equal(optimizedBytes.length % 4, 0);
    assert.equal(parsed.jsonChunkLength % 4, 0);
    assert.equal(parsed.binary.length % 4, 0);
    const validation = await validateGlb(optimizedBytes, 2048);
    assert.equal(validation.images[0].width, 2048);
    assert.equal(validation.images[0].height, 512);
  } finally {
    rmSync(directory, { recursive: true, force: true });
  }
});

test("leaves an image byte-identical when it is already within the limit", async () => {
  const directory = mkdtempSync(path.join(tmpdir(), "glb-opt-small-"));
  try {
    const pngPath = path.join(directory, "small.png");
    const inputPath = path.join(directory, "input.glb");
    const outputPath = path.join(directory, "output.glb");
    execFileSync("magick", ["-size", "64x32", "xc:#884422", pngPath]);
    const png = readFileSync(pngPath);
    const document = {
      asset: { version: "2.0" },
      buffers: [{ byteLength: png.length }],
      bufferViews: [{ buffer: 0, byteOffset: 0, byteLength: png.length }],
      images: [{ bufferView: 0, mimeType: "image/png" }],
    };
    writeFileSync(inputPath, createGlb(document, png));

    await optimizeGlb(inputPath, outputPath, { maxDimension: 2048 });

    const parsed = parseGlb(readFileSync(outputPath));
    const view = parsed.document.bufferViews[0];
    assert.deepEqual(parsed.binary.subarray(view.byteOffset, view.byteOffset + view.byteLength), png);
  } finally {
    rmSync(directory, { recursive: true, force: true });
  }
});

