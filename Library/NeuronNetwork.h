#pragma once
#include "Neuron.h"
#include <iostream>
#include <iomanip>
#include <fstream>

class NeuronNetwork {
public:

    int layers;
    int* size;
    Neuron** neurons;
    double*** weights;

    NeuronNetwork(int n, int* p) {

        layers  = n;
        size    = new int[n];
        neurons = new Neuron * [n];
        weights = new double** [n - 1];


        for (int i = 0; i < n; i++) {

            size[i] = p[i];
            neurons[i] = new Neuron[p[i]+1];

            neurons[i][p[i]].ActiveValue = 1.0; // Bias active value is only 1

            if(i == n-1) break; // Because bias has n-1 layers 
            weights[i] = new double* [p[i]+1];// +1 for bias weights
            for (int j = 0; j <= p[i]; j++) { 
                weights[i][j] = new double[p[i + 1]]; // i+1 because next layer
                for(int k = 0;k < p[i+1];k++){
                    weights[i][j][k] = (rand() % 100) * 0.01; // Set random value for weights
                }
            }

        }

    }
    ~NeuronNetwork() {
        delete[] size, neurons, weights;
    }

    void SetInput(double* p) {
        for (int i = 0; i < size[0]; i++) {
            neurons[0][i].ActiveValue = p[i];
        }
    }

    void ForwardFeed() {

        for (int i = 1; i < layers; i++) {
            for (int j = 0; j < size[i]; j++) {
                neurons[i][j].value = 0.0;
                for (int k = 0; k <= size[i - 1]; k++) {
                    neurons[i][j].value += neurons[i - 1][k].ActiveValue * weights[i - 1][k][j];
                }
                neurons[i][j].Activate();
            }
        }
            
    }
    int Predict() {

        double MaximalValue =  neurons[layers - 1][0].ActiveValue;
        int MaxIndex = 0;

        for (int i = 1; i < size[layers - 1]; i++) {
            if (MaximalValue < neurons[layers - 1][i].ActiveValue) {
                MaximalValue = neurons[layers - 1][i].ActiveValue;
                MaxIndex = i;
            }
        }

        return MaxIndex;
    }

    double ErrorCouter(double* ra) {

        double sum = 0.0;

        for (int i = 0; i < size[layers - 1]; i++) {
            sum += pow((neurons[layers - 1][i].ActiveValue - ra[i]), 2);
        }

        return sum;
    }

    void BackPropagation(double* ra, double ls) {

        for (int i = 0; i < size[layers - 1]; i++) {
            neurons[layers - 1][i].error = 2 * (neurons[layers - 1][i].ActiveValue - ra[i]);
        }

        for (int i = layers - 2; i >= 0; i--) {
            for (int j = 0; j < size[i]+1; j++) {
                neurons[i][j].error = 0.0;
                for (int k = 0; k < size[i + 1]; k++) {
                    neurons[i][j].error   += (neurons[i + 1][k].error) * (neurons[i + 1][k].proiz()) * (weights[i][j][k]);
                    weights[i][j][k] -= ls * (neurons[i + 1][k].error) * (neurons[i + 1][k].proiz()) * (neurons[i][j].ActiveValue);
                }
            }
        }
    }

    void SaveNetwork(const char *filename) {
        std::ofstream fout(filename);
        for (int i = 0; i < layers - 1; i++) {
            for (int j = 0; j < size[i]; j++) {
                for (int k = 0; k < size[i + 1]; k++) {
                    fout << std::fixed << std::setprecision(8) << weights[i][j][k] << ' ';
                }
                fout << '\n';
            }
        }
        fout << '\n';
        for(int i = 0;i < layers-1;i++){
            for(int j = 0;j < size[i+1];j++){
                fout << weights[i][size[i]][j] << ' ';
            }
        }
        fout.close();
    }
    void LoadNetwork(const char *filename) {
        std::ifstream fin(filename);
        for (int i = 0; i < layers - 1; i++) {
            for (int j = 0; j < size[i]; j++) {
                for (int k = 0; k < size[i + 1]; k++) {
                    fin >> weights[i][j][k]; 
                }
            }
        }
        for(int i = 0;i < layers-1;i++){
            for(int j = 0;j < size[i+1];j++){
                fin >> weights[i][size[i]][j];
            }
        }
        fin.close();
    }

    double* SoftMax(){

        double SE = 0;
        double* ans = new double[size[layers-1]];

        for(int i = 0;i < size[layers-1];i++){
            ans[i] = exp(neurons[layers-1][i].value);
            SE += ans[i];
        }

        for(int i = 0;i < size[layers-1];i++){
            ans[i] = ans[i]/SE*100.0;
        }

        return ans;

    }

};
