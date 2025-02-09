﻿using IATK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SSVis
{
    public class DataVisualisationManager : MonoBehaviour
    {
        public static DataVisualisationManager Instance { get; private set; }

        public DataSource DataSource;
        public DataSource NetworkDataSource;
        public DataSource TemporalDataSource;

        [Header("Default Visualisation Properties")]
        public Color VisualisationColour = Color.white;
        public float VisualisationSize = 0.1f;
        public Vector3 VisualisationScale = new Vector3(0.25f, 0.25f, 0.25f);

        public UnityEvent OnVisualisationCreated;
        public UnityEvent OnVisualisationsDestroyed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if (DataSource == null)
                Debug.LogError("You must assign a datasource to DataVisualisationManager!");
        }

        public DataVisualisation CreateDataVisualisation(DataSource dataSource, AbstractVisualisation.VisualisationTypes visualisationType, AbstractVisualisation.GeometryType geometryType,
            string xDimension = "Undefined", string yDimension = "Undefined", string zDimension = "Undefined", float size = 1, string sizeDimension = "Undefined", Color color = default(Color), string colourDimension = "Undefined", Vector3 scale = default(Vector3),
            BarAggregation barAggregation = BarAggregation.Count, int numXBins = 2, int numZBins = 2
        )
        {
            DataVisualisation vis = Instantiate(Resources.Load("DataVisualisation") as GameObject).GetComponent<DataVisualisation>();

            vis.ID = Guid.NewGuid().ToString();
            vis.Visualisation.dataSource = dataSource;
            vis.Visualisation.visualisationType = visualisationType;
            vis.Visualisation.geometry = geometryType;
            vis.Visualisation.xDimension = xDimension;
            vis.Visualisation.yDimension = yDimension;
            vis.Visualisation.zDimension = zDimension;
            vis.Visualisation.size = size;
            vis.Visualisation.sizeDimension = sizeDimension;
            vis.Visualisation.colour = color != default(Color) ? color : Color.white;
            vis.Visualisation.colourDimension = colourDimension;
            vis.Visualisation.barAggregation = barAggregation.ToString();
            vis.Visualisation.numXBins = numXBins;
            vis.Visualisation.numZBins = numZBins;
            vis.Visualisation.CreateVisualisation(visualisationType);
            vis.Scale = scale != default(Vector3) ? scale : Vector3.one;

            OnVisualisationCreated.Invoke();

            return vis;
        }

        public DataVisualisation CloneDataVisualisation(DataVisualisation dataVisualisation)
        {
            DataVisualisation vis = CreateDataVisualisation(dataVisualisation.DataSource,
                                                            dataVisualisation.VisualisationType,
                                                            dataVisualisation.GeometryType,
                                                            dataVisualisation.XDimension,
                                                            dataVisualisation.YDimension,
                                                            dataVisualisation.ZDimension,
                                                            dataVisualisation.Size,
                                                            dataVisualisation.SizeByDimension,
                                                            dataVisualisation.Colour,
                                                            dataVisualisation.ColourByDimension,
                                                            dataVisualisation.Scale,
                                                            dataVisualisation.BarAggregation,
                                                            dataVisualisation.NumXBins,
                                                            dataVisualisation.NumZBins
                                                            );

            vis.transform.position = dataVisualisation.transform.position;
            vis.transform.rotation = dataVisualisation.transform.rotation;
            vis.transform.localScale = dataVisualisation.transform.localScale;

            return vis;
        }

        public void CreateRandom2DScatterplot()
        {
            // Set random dimensions
            System.Random random = new System.Random(System.DateTime.Now.Millisecond);
            int numDimensions = DataSource.DimensionCount;
            string xDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            string yDimension = DataSource[random.Next(0, numDimensions)].Identifier;

            DataVisualisation vis = CreateDataVisualisation(DataSource, AbstractVisualisation.VisualisationTypes.SCATTERPLOT, AbstractVisualisation.GeometryType.Points, xDimension: xDimension, yDimension: yDimension, size: VisualisationSize, scale: VisualisationScale);

            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void CreateRandom3DScatterplot()
        {
            // Set random dimensions
            System.Random random = new System.Random(System.DateTime.Now.Millisecond);
            int numDimensions = DataSource.DimensionCount;
            string xDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            string yDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            string zDimension = DataSource[random.Next(0, numDimensions)].Identifier;

            DataVisualisation vis = CreateDataVisualisation(DataSource, AbstractVisualisation.VisualisationTypes.SCATTERPLOT, AbstractVisualisation.GeometryType.Points, xDimension: xDimension, yDimension: yDimension, zDimension: zDimension, size: VisualisationSize, scale: VisualisationScale);

            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void CreateRandomHistogram()
        {
            // Set random dimensions
            System.Random random = new System.Random(System.DateTime.Now.Millisecond);
            int numDimensions = DataSource.DimensionCount;
            string xDimension = DataSource[random.Next(0, numDimensions)].Identifier;

            DataVisualisation vis = CreateDataVisualisation(DataSource, AbstractVisualisation.VisualisationTypes.BAR, AbstractVisualisation.GeometryType.Bars, xDimension: xDimension, scale: VisualisationScale, barAggregation: BarAggregation.Count, numXBins: 5, numZBins: 5);

            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void CreateRandom2DBarChart()
        {
            // Set random dimensions
            System.Random random = new System.Random(System.DateTime.Now.Millisecond);
            int numDimensions = DataSource.DimensionCount;
            string xDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            string yDimension = DataSource[random.Next(0, numDimensions)].Identifier;

            DataVisualisation vis = CreateDataVisualisation(DataSource, AbstractVisualisation.VisualisationTypes.BAR, AbstractVisualisation.GeometryType.Bars, xDimension: xDimension, yDimension: yDimension, scale: VisualisationScale, barAggregation: BarAggregation.Sum, numXBins: 5, numZBins: 5);

            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void CreateRandom3DBarChart()
        {
            // Set random dimensions
            System.Random random = new System.Random(System.DateTime.Now.Millisecond);
            int numDimensions = DataSource.DimensionCount;
            string xDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            string yDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            string zDimension = DataSource[random.Next(0, numDimensions)].Identifier;

            DataVisualisation vis = CreateDataVisualisation(DataSource, AbstractVisualisation.VisualisationTypes.BAR, AbstractVisualisation.GeometryType.Bars, xDimension: xDimension, yDimension: yDimension, zDimension:zDimension, scale: VisualisationScale, barAggregation: BarAggregation.Sum, numXBins: 5, numZBins: 5);

            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void CreateNetworkGraph()
        {
            DataVisualisation vis = CreateDataVisualisation(NetworkDataSource, AbstractVisualisation.VisualisationTypes.SCATTERPLOT, AbstractVisualisation.GeometryType.LinesAndDots, xDimension: "Longitude", yDimension: "Latitude", size: 0.1f, scale: VisualisationScale);
            
            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void CreateVolumeRendering()
        {
            GameObject volume = GameObject.Instantiate(Resources.Load("VolumeRendering")) as GameObject;

            volume.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.4f;
        }

        public void CreateTemporal2DScatterplot()
        {
            DataVisualisation vis = CreateDataVisualisation(TemporalDataSource, AbstractVisualisation.VisualisationTypes.SCATTERPLOT, AbstractVisualisation.GeometryType.Points, xDimension: "Longitude", yDimension: "Latitude", size: VisualisationSize, scale: VisualisationScale);

            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void DestroyAllDataVisualisations()
        {
            var visualisations = GameObject.FindGameObjectsWithTag("DataVisualisation");
            foreach (var vis in visualisations)
            {
                Destroy(vis.gameObject);
            }

            var linkingVisualisations = GameObject.FindObjectsOfType<LinkingVisualisations>();
            foreach (var vis in visualisations)
            {
                Destroy(vis.gameObject);
            }

            OnVisualisationsDestroyed.Invoke();

            var volumes = FindObjectsOfType<VolumeRendering.VolumeRendering>();
            foreach (var volume in volumes)
            {
                Destroy(volume.gameObject);
            }
        }

    }
}